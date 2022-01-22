using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Framework;
using BizleMe.Interfaces.Shared;
using System;
using System.Collections.Generic;
using System.Transactions;
using BizleMeAccounting.DAL;
using BizleMeAccounting.DAL.Repositories;
using BizleMeAccounting.DAL.DObjects.AccountingPlan;
using BizleMe.DAL.Repositories;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public class AddAccountingImpact : IAddAccountingImpact
    {
        public AddAccountingImpactResponse Execute(AddAccountingImpactRequest addAccountingImpactRequest)
        {
            AddAccountingImpactResponse addAccountingImpactResponse = new AddAccountingImpactResponse()
            {
                Status = new Status()
            };
            using (TransactionScope scope = new TransactionScope())
            {
                using (AccountingUOW uow = new AccountingUOW())
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(addAccountingImpactRequest.AccountingImpact.AccountingImpactNumber))
                        {
                            throw new Exception(ErrorCode.FieldCanNotBeEmpty.ToString() + "|" + "Please complete required fields");
                        }
                        //verifico se esiste un  account impact con lo stesso number 
                        var sameAccountImpact = uow.GetRepository<AccountingPlansRepository>().GetSameAccountingImpact(0,addAccountingImpactRequest.AccountingImpact.AccountingImpactNumber);
                        if (sameAccountImpact != null)
                        {
                            throw new Exception(ErrorCode.AccountingImpactAlreadyExist.ToString() + "|" + "An accounting impact with this number already exists ");
                        }
                        BizleMeAccounting.DAL.DObjects.AccountingPlan.AccountingImpact accountingImpact = new BizleMeAccounting.DAL.DObjects.AccountingPlan.AccountingImpact()
                        {
                            Deleted = false,
                            Enabled = true,
                            VatImpact = addAccountingImpactRequest.AccountingImpact.VatImpact,
                            DocumentImpact = addAccountingImpactRequest.AccountingImpact.DocumentImpact,
                            AccountingImpactNumber = addAccountingImpactRequest.AccountingImpact.AccountingImpactNumber
                        };
                        uow.GetRepository<AccountingPlansRepository>().AddAccountingImpact(accountingImpact);
                        uow.Save();
                        addAccountingImpactResponse.Code = accountingImpact.Code;

                        //  Verifichiamo che la lunghezza della descrizione non e più di 200 chars
                        if (!string.IsNullOrWhiteSpace(addAccountingImpactRequest.AccountingImpact.Description))
                        {
                            if (addAccountingImpactRequest.AccountingImpact.Description.Length > 200)
                            {
                                throw new Exception(ErrorCode.FieldMustContain200CharactersOrLess.ToString() + "|" + "Accounting impact description must be 200 characters or less");
                            }
                        }

                        Tsl_AccountingImpact tsl_AccountingImpact = new Tsl_AccountingImpact()
                        {
                            AccountingImpactCode = accountingImpact.Code,
                            Description = addAccountingImpactRequest.AccountingImpact.Description,
                            LanguageCode = addAccountingImpactRequest.AccountingImpact.TslLanguageCode
                        };

                        uow.GetRepository<AccountingPlansRepository>().AddTslAccountingImpact(tsl_AccountingImpact);
                     
                        //aggiunta del subAccounts legato a questo causale contabile 
                        if(addAccountingImpactRequest.AccountingImpact.ImpactSubAccounts != null)
                        {
                            foreach (var account in addAccountingImpactRequest.AccountingImpact.ImpactSubAccounts)
                            {
                                BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount impactSubAccount = new BizleMeAccounting.DAL.DObjects.AccountingPlan.ImpactSubAccount
                                {
                                    SubAccountCode = account.SubAccountCode,
                                    SignCode = account.SignCode,
                                    AccountingImpactCode = accountingImpact.Code
                                };
                                uow.GetRepository<AccountingPlansRepository>().AddImpactSubAccounts(impactSubAccount); 
                            }
                        }
                        uow.Save();
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
                        addAccountingImpactResponse.Status = new Status(code, description, type, value);
                    }
                }
            }
            return addAccountingImpactResponse;
        }
    }
}
