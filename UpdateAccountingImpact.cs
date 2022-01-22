using BizleMeAccounting.DAL;
using BizleMeAccounting.DAL.Repositories;
using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Framework;
using BizleMe.Interfaces.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Transactions;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public class UpdateAccountingImpact : IUpdateAccountingImpact
    {
        public UpdateAccountingImpactResponse Execute(UpdateAccountingImpactRequest updateAccountingImpactRequest)
        {
            UpdateAccountingImpactResponse updateAccountingImpactResponse = new UpdateAccountingImpactResponse()
            {
                Status = new Status()
            };
            using (TransactionScope scope = new TransactionScope())
            {

                using (AccountingUOW uow = new AccountingUOW())
                {
                    try
                    {
                        List<BizleMeAccounting.DAL.DObjects.AccountingPlan.Tsl_AccountingImpact> impactTranslations = new List<BizleMeAccounting.DAL.DObjects.AccountingPlan.Tsl_AccountingImpact>();
                        //dobbiamo prendere il default language della sistema

                        int languageCode = int.Parse(ConfigurationManager.AppSettings["en-GB"]);

                        if (updateAccountingImpactRequest.TranslatedAccountingImpacts != null)
                        {
                            foreach (var translation in updateAccountingImpactRequest.TranslatedAccountingImpacts)
                            {
                                // Verifichiamo che la lunghezza della descrizione non e più di 200 chars
                                if (!string.IsNullOrWhiteSpace(translation.Description))
                                {
                                    if (translation.Description.Length > 200)
                                    {
                                        throw new Exception(ErrorCode.FieldMustContain200CharactersOrLess.ToString() + "|" + "Accounting impact description must be 200 characters or less");
                                    }
                                }
                                //prendo nella schema tslAccountingImpact la riga che ha il languageCode  della richiesta 
                                var existTranslation = uow.GetRepository<AccountingPlansRepository>().GetTslAccountingImpactByLanguage(translation.AccountingImpactCode, updateAccountingImpactRequest.TslLanguageCode);
                                if (existTranslation == null)
                                {
                                    BizleMeAccounting.DAL.DObjects.AccountingPlan.Tsl_AccountingImpact tsl_AccountingImpact = new BizleMeAccounting.DAL.DObjects.AccountingPlan.Tsl_AccountingImpact()
                                    {
                                        AccountingImpactCode = translation.AccountingImpactCode,
                                        Description = translation.Description,
                                        LanguageCode = updateAccountingImpactRequest.TslLanguageCode
                                    };
                                    uow.GetRepository<AccountingPlansRepository>().AddTslAccountingImpact(tsl_AccountingImpact);
                                    translation.Code = tsl_AccountingImpact.Code;
                                    uow.Save();
                                }
                                //atrimenti facciamo update di quello che abbiamo trovato 
                                else
                                {
                                    existTranslation.Description = translation.Description;
                                }
                                //se languageCode della richiesta è il languageCode default della sistema facciamo update dei dati della tabella base AccountingImppact
                                if (updateAccountingImpactRequest.TslLanguageCode == languageCode && updateAccountingImpactRequest.Code != 0)
                                {
                                    if (string.IsNullOrWhiteSpace(updateAccountingImpactRequest.AccountingImpactNumber))
                                    {
                                        throw new Exception(ErrorCode.FieldCanNotBeEmpty.ToString() + "|" + "Please complete required fields");
                                    }
                                    //verifico se esiste un  account impact con lo stesso number 
                                    var sameAccountImpact = uow.GetRepository<AccountingPlansRepository>().GetSameAccountingImpact(updateAccountingImpactRequest.Code, updateAccountingImpactRequest.AccountingImpactNumber);
                                    if (sameAccountImpact != null)
                                    {
                                        throw new Exception(ErrorCode.AccountingImpactAlreadyExist.ToString() + "|" + "An accounting impact with this number already exists ");
                                    }
                                    var accountImpactTranslation = uow.GetByCode<BizleMeAccounting.DAL.DObjects.AccountingPlan.AccountingImpact>(updateAccountingImpactRequest.Code);
                                    accountImpactTranslation.AccountingImpactNumber = updateAccountingImpactRequest.AccountingImpactNumber;
                                    accountImpactTranslation.Enabled = updateAccountingImpactRequest.Enabled;
                                    accountImpactTranslation.Deleted = updateAccountingImpactRequest.Deleted;
                                    accountImpactTranslation.DocumentImpact = updateAccountingImpactRequest.DocumentImpact;
                                    accountImpactTranslation.VatImpact = updateAccountingImpactRequest.VatImpact;

                                  
                                    #region RemoveImpactSubAccount
                                    //prendo i dati nel db
                                    var impactAccounts = uow.GetRepository<AccountingPlansRepository>().GetImpactSubAccounts(updateAccountingImpactRequest.Code);
                                    List<BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount> toDelete = new List<BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount>();
                                    foreach (var account in impactAccounts)
                                    {
                                        //facciamo remove del accounts che non vengono nella richiesta
                                        var exist = updateAccountingImpactRequest.ImpactSubAccounts.Find(x => x.SubAccountCode == account.SubAccountCode);
                                        if (exist == null)
                                        {
                                            //verifico se esiste un ImpactAccount con quello SubAccountCode
                                            var remove = uow.GetRepository<AccountingPlansRepository>().GetImpactSubAccount(updateAccountingImpactRequest.Code, account.SubAccountCode);

                                            if (remove != null)
                                            {
                                                toDelete.Add(remove);
                                            }
                                        }
                                    }
                                    uow.GetRepository<AccountingPlansRepository>().RemoveImpactSubAccounts(toDelete);
                                    uow.Save();
                                    #endregion

                                    #region AddImpactSubAccount
                                    List<BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount> impactAccountList = new List<BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount>();
                                    if(updateAccountingImpactRequest.ImpactSubAccounts != null)
                                    {
                                        foreach (var account in updateAccountingImpactRequest.ImpactSubAccounts)
                                        {
                                            //verifico se esiste un impactSubAccount con questo accountImpactCode e SubAccountCode 
                                            var impactAccount = uow.GetRepository<AccountingPlansRepository>().GetImpactSubAccount(updateAccountingImpactRequest.Code, account.SubAccountCode);
                                            // se questa subAccount  non  è associato con questo accountImpact, la aggiungiamo nella tabella ImpactSubAccount
                                            if (impactAccount == null)
                                            {

                                                BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount impactSubAccount = new BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount
                                                {
                                                    SubAccountCode = account.SubAccountCode,
                                                    SignCode = account.SignCode,
                                                    AccountingImpactCode = updateAccountingImpactRequest.Code
                                                };
                                                uow.GetRepository<AccountingPlansRepository>().AddImpactSubAccounts(impactSubAccount);
                                            }
                                            uow.Save();
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                        scope.Complete();
                    }
                    catch (Exception ex)
                    {

                        scope.Dispose();
                        uow.RollBack();
                        int code = int.Parse(ex.Message.Split('|')[0]);
                        string description = ex.Message.Split('|')[1];
                        string type = "Exception";
                        string value = "Operation could not be completed";
                        updateAccountingImpactResponse.Status = new Status(code, description, type, value);
                    }
                }
            }
            return updateAccountingImpactResponse;
        }
    }
}
