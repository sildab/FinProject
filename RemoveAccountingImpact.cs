using BizleMeAccounting.DAL;
using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMe.Interfaces.Shared;
using System;
using BizleMeAccounting.DAL.Repositories;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public  class RemoveAccountingImpact : IRemoveAccountingImpact
    {
        public RemoveAccountingImpactResponse Execute(RemoveAccountingImpactRequest removeAccountingImpactRequest)
        {
            RemoveAccountingImpactResponse removeAccountingImpactResponse = new RemoveAccountingImpactResponse()
            {
               Status = new Status()
            };
            using (AccountingUOW uow = new AccountingUOW())
            {
                try
                {
                    if (removeAccountingImpactRequest.Code != 0)
                    {
                        var accountingImpact = uow.GetRepository<AccountingPlansRepository>().GetAccountingImpactByCode(removeAccountingImpactRequest.Code);
                        if (accountingImpact != null)
                        {
                            accountingImpact.Deleted = true;
                            uow.Save();
                        }
                    }
                }
                catch (Exception ex)
                {
                    int code = int.Parse(ex.Message.Split('|')[0]);
                    string description = ex.Message.Split('|')[1];
                    string type = "Exception";
                    string value = "Operation could not be completed";
                    removeAccountingImpactResponse.Status = new Status(code, description, type, value);
                }
            }
            return removeAccountingImpactResponse;
        }
    }
}
