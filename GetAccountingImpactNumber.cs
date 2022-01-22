using BizleMeAccounting.DAL;
using BizleMeAccounting.DTOs.AccountingPlans.AccountingImpact;
using BizleMeAccounting.Interface.AccountingPlans.AccountingImpact;
using BizleMe.Interfaces.Shared;
using System;
using System.Linq;

namespace BizleMeAccounting.Common.AccountingPlans.AccountingImpact
{
    public class GetAccountingImpactNumber : IGetAccountingImpactNumber
    {
        public GetAccountingImpactNumberResponse Execute(GetAccountingImpactNumberRequest getAccountingImpactNumberRequest)
        {
            GetAccountingImpactNumberResponse getAccountingImpactNumberResponse = new GetAccountingImpactNumberResponse()
            {
                Status = new Status()
            };
            using (AccountingUOW uow = new AccountingUOW())
            {
                try
                {
                    var allAccountingImpacts = uow.GetAll<BizleMeAccounting.DAL.DObjects.AccountingPlan.AccountingImpact>().Where(d=>d.Deleted != true).ToList();
                    if (allAccountingImpacts.Count == 0)
                    {
                        getAccountingImpactNumberResponse.AccountingImpactNumber = "1";
                    }
                    else
                    {
                        string lastNumber = allAccountingImpacts.OrderByDescending(d => d.AccountingImpactNumber).FirstOrDefault().AccountingImpactNumber;

                        var nrLength = lastNumber.ToArray().Count();                    //conta quanti numeri ci sono
                        long converted = long.Parse(lastNumber);                        // convert in long
                        long sum = converted + 1;                                      //somma i numeri diversi da zero con 1
                        string convertedSum = sum.ToString();                          // convert in string
                        var nextNumber = convertedSum.PadLeft(nrLength, '0');           //aggiunge 0 come lunghezza del numero in db


                        getAccountingImpactNumberResponse.AccountingImpactNumber = nextNumber;
                    }
                }
                catch (Exception ex)
                {

                    int code = int.Parse(ex.Message.Split('|')[0]);
                    string description = ex.Message.Split('|')[1];
                    string type = "Exception";
                    string value = "Operation could not be completed";
                    getAccountingImpactNumberResponse.Status = new Status(code, description, type, value);
                }
            }
            return getAccountingImpactNumberResponse;
        }
    }
    
}
