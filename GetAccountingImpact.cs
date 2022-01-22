using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Framework;
using BizleMe.Interfaces.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using BizleMeAccounting.DAL;
using BizleMeAccounting.DAL.Repositories;
using BizleMeAccounting.DAL.DObjects.Accounts;
using BizleMe.DAL.Repositories;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public  class GetAccountingImpact : IGetAccountingImpact
    {
        public GetAccountingImpactResponse Execute(GetAccountingImpactRequest getAccountingImpactRequest)
        {
            GetAccountingImpactResponse getAccountingImpactResponse = new GetAccountingImpactResponse()
            {
                Status = new Status(),
                AccountingImpact = new AccountingImpactBO()
            };
            using (AccountingUOW uow = new AccountingUOW())
            {
                try
                {
                    int languageCode = int.Parse(ConfigurationManager.AppSettings["en-GB"]);
                    if (uow.GetRepository<AccountingPlansRepository>().GetAccountingImpactByCode(getAccountingImpactRequest.Code) == null)
                    {
                        throw new Exception(ErrorCode.AccountingImpactDoesNotExist.ToString() + "|" + "Accounting impact does not exist.");
                    }
                    else
                    {
                        var accountingImpact = uow.GetRepository<AccountingPlansRepository>().GetAccountingImpactByCode(getAccountingImpactRequest.Code);
                        var impactTranslation = uow.GetRepository<AccountingPlansRepository>().GetTslAccountingImpactByLanguage(accountingImpact.Code, getAccountingImpactRequest.TslLanguageCode);
                        //prendo la lista del accounts legato con questo account impact
                        List<ImpactSubAccount> accounts = new List<ImpactSubAccount>();
                        var impactSubAccounts = uow.GetRepository<AccountingPlansRepository>().GetImpactSubAccounts(accountingImpact.Code);
                        if (impactSubAccounts != null)
                        {
                            foreach (var account in impactSubAccounts)
                            {
                                accounts.Add(new ImpactSubAccount
                                {
                                    Code = account.Code,
                                    SignCode = account.SignCode,
                                    SignName = uow.GetRepository<MasterAccountsRepository>().GetTslSignByLanguage(languageCode, account.SignCode).Name,
                                    SubAccountCode = account.SubAccountCode,
                                    SubAccountNumber = uow.GetByCode<SubAccount>(account.SubAccountCode).SubAccountNumber,
                                    SubAccountDescription = uow.GetRepository<MasterAccountsRepository>().GetTslSubAccountbyLanguageCode(languageCode, account.SubAccountCode).Description,
                                    AccountingImpactCode = account.AccountingImpactCode
                                });
                            }
                        }
                        using (UnitOfWork uow1 = new UnitOfWork())
                        {
                            getAccountingImpactResponse.AccountingImpact = new AccountingImpactBO()
                            {
                                Code = accountingImpact.Code,
                                Description = impactTranslation != null ? impactTranslation.Description :  null,
                                TranslationCode = impactTranslation != null ? impactTranslation.Code : 0,
                                TslLanguageCode = impactTranslation != null ?  impactTranslation.LanguageCode : 0,
                                TslLanguageName = impactTranslation != null ? uow1.GetRepository<SharedRepository>().GetTslLanguageName(impactTranslation.LanguageCode).Name : null,
                                AccountingImpactNumber = accountingImpact.AccountingImpactNumber,
                                DocumentImpact = accountingImpact.DocumentImpact,
                                Deleted = accountingImpact.Deleted,
                                Enabled = accountingImpact.Enabled,
                                VatImpact = accountingImpact.VatImpact,
                                ImpactSubAccounts = accounts
                            };
                        }
                    }                
                }
                catch (Exception ex)
                {
                    uow.RollBack();
                    int code = int.Parse(ex.Message.Split('|')[0]);
                    string description = ex.Message.Split('|')[1];
                    string type = "Exception";
                    string value = "Operation could not be completed";
                    getAccountingImpactResponse.Status = new Status(code, description, type, value);
                }
            }
            return getAccountingImpactResponse;
        }
    }
}
