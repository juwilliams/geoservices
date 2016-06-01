using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Cars
{
    public class CarsReport
    {
        #region Members

        public string EventId { get; set; }

        public string MessageDate { get; set; }

        public string MessageTime { get; set; }

        public string UtcOffset { get; set; }

        public string ExpirationDate { get; set; }

        public string ExpirationTime { get; set; }

        public string UpdateNumber { get; set; }

        public string Priority { get; set; }

        public string Headline { get; set; }

        public string LinkOwnership { get; set; }

        public string Route { get; set; }

        public string AffectedCounties { get; set; }

        public string FromIntersection { get; set; }

        public string ToIntersection { get; set; }

        public string StartDate { get; set; }

        public string StartTime { get; set; }

        public string EndDate { get; set; }

        public string EndTime { get; set; }

        public string UpdateDate { get; set; }

        public string UpdateTime { get; set; }

        public string UpdateUtcOffset { get; set; }

        public string PrimaryLat { get; set; }

        public string PrimaryLong { get; set; }

        #endregion


        #region Factory

        public static List<CarsReport> Parse(JArray jObjects)
        {
            List<CarsReport> reports = new List<CarsReport>();

            for (int i = 0; i < jObjects.Count; i++)
            {
                reports.Add(CarsReport.Create(jObjects[i]));
            }

            return reports;
        }

        public static CarsReport Create(dynamic jObject)
        {
            return new CarsReport()
            {
                EventId = (string)jObject.EventId,
                MessageDate = (string)jObject.MessageDate,
                MessageTime = (string)jObject.MessageTime,
                UtcOffset = (string)jObject.UtcOffset,
                ExpirationDate = (string)jObject.ExpirationDate,
                ExpirationTime = (string)jObject.ExpirationTime,
                UpdateNumber = (string)jObject.UpdateNumber,
                Priority = (string)jObject.Priority,
                Headline = (string)jObject.Headline,
                LinkOwnership = (string)jObject.LinkOwnership,
                Route = (string)jObject.Route,
                AffectedCounties = (string)jObject.AffectedCounties,
                FromIntersection = (string)jObject.FromIntersection,
                ToIntersection = (string)jObject.ToIntersection,
                StartDate = (string)jObject.StartDate,
                StartTime = (string)jObject.StartTime,
                EndDate = (string)jObject.EndDate,
                EndTime = (string)jObject.EndTime,
                UpdateDate = (string)jObject.UpdateDate,
                UpdateTime = (string)jObject.UpdateTime,
                UpdateUtcOffset = (string)jObject.UpdateUtcOffset,
                PrimaryLat = (string)jObject.PrimaryLat,
                PrimaryLong = (string)jObject.PrimaryLong
            };
        }

        #endregion
    }
}
