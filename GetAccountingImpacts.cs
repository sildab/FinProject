using BizleMeAccounting.DAL;
using BizleMeAccounting.DAL.DObjects.AccountingPlan;
using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMe.Interfaces.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using BizleMeAccounting.DAL.Repositories;
using BizleMe.DAL.Repositories;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public class GetAccountingImpacts : IGetAccountingImpacts
    {
        public GetAccountingImpactsResponse Execute(GetAccountingImpactsRequest getAccountingImpactsRequest)
        {
            GetAccountingImpactsResponse getAccountingImpactsResponse = new GetAccountingImpactsResponse()
            {
                Status = new Status(),
                AccountingImpactsList = new List<AccountingImpactsList>()
            };
            using (AccountingUOW uow = new AccountingUOW())
            {
                try
                {
                    int disabled = 1;
                    if (getAccountingImpactsRequest.DisabledToo == false)
                    {
                        disabled = 0;
                    }
                    int deleted = 1;
                    if (getAccountingImpactsRequest.Deleted == false)
                    {
                        deleted = 0;
                    }
                    using (UnitOfWork uow1 = new UnitOfWork())
                    {
                        //dobbiamo prendere il default language della sistema 
                      int languageCode = int.Parse(ConfigurationManager.AppSettings["en-GB"]);

                        var accountingImpacts = (from accountingImpact in uow.GetAll<BizleMeAccounting.DAL.DObjects.AccountingPlan.AccountingImpact>()
                                             join accountingTranslations in uow.GetAll<Tsl_AccountingImpact>()
                                              on accountingImpact.Code equals accountingTranslations.AccountingImpactCode
                                             where
                                            ((accountingTranslations.LanguageCode == languageCode)
                                              && (accountingTranslations.Description.Contains(getAccountingImpactsRequest.Keyword)
                                              || getAccountingImpactsRequest.Keyword == string.Empty
                                              || getAccountingImpactsRequest.Keyword == null )
                                              && ((disabled == 0 ? accountingImpact.Enabled == true : accountingImpact.Enabled == true || accountingImpact.Enabled == false)
                                              && (deleted == 0 ? accountingImpact.Deleted == false : accountingImpact.Deleted == true)))
                                             select new
                                             {
                                                 AccountingImpactCode = accountingImpact.Code,
                                                 AccountingImpactNr = accountingImpact.AccountingImpactNumber,
                                                 AccountingImpactDeleted = accountingImpact.Deleted,
                                                 AccountingImpactEnabled = accountingImpact.Enabled,
                                                 AccountingImpactVatImpact = accountingImpact.VatImpact,
                                                 AccountingImpactDocImpact = accountingImpact.DocumentImpact
                                             }).ToList().OrderBy(d=>d.AccountingImpactNr);
                    if (accountingImpacts.Count() > 0)
                    {
                            foreach (var accountingImpact in accountingImpacts)
                            {
                                //verificho se esiste un translation per il languageCode selezionato
                                var translation = uow.GetRepository<AccountingPlansRepository>().GetTslAccountingImpactByLanguage(accountingImpact.AccountingImpactCode, getAccountingImpactsRequest.TslLanguageCode);
                                string languageInfo = getAccountingImpactsRequest.TslLanguageCode != 0 ? uow1.GetRepository<SharedRepository>().GetTSLLanguageAdded(getAccountingImpactsRequest.TslLanguageCode, languageCode).Name
                                                       : uow1.GetRepository<SharedRepository>().GetTslLanguageName(languageCode).Name;
                                getAccountingImpactsResponse.AccountingImpactsList.Add(new AccountingImpactsList
                                {
                                    Code = accountingImpact.AccountingImpactCode,
                                    Description = uow.GetRepository<AccountingPlansRepository>().GetTslAccountingImpactByLanguage(accountingImpact.AccountingImpactCode, languageCode).Description,
                                    TranslatedCode = translation != null ? translation.Code : 0,
                                    TslLanguageCode = translation != null ? translation.LanguageCode : 0,
                                    TslLanguageName = languageInfo,
                                    TranslatedDescription = translation != null ? translation.Description : null,
                                    Deleted = accountingImpact.AccountingImpactDeleted,
                                    Enabled = accountingImpact.AccountingImpactEnabled,                                    
                                    VatImpact = accountingImpact.AccountingImpactVatImpact,
                                    DocumentImpact = accountingImpact.AccountingImpactDocImpact,
                                    AccountingImpactNumber = accountingImpact.AccountingImpactNr
                                    
                                });
                            }
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
                    getAccountingImpactsResponse.Status = new Status(code, description, type, value);
                }
            }
            return getAccountingImpactsResponse;
        }
    }
}
