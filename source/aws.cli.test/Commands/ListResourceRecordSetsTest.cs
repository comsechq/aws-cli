﻿using Amazon;
using Aws.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace Aws.Commands
{
    [TestFixture]
    public class ListResourceRecordSetsTest
    {
        private ListResourceRecordSets command;

        private Mock<IRoute53Service> route53ServiceMock;

        [SetUp]
        public void Setup()
        {
            route53ServiceMock = new Mock<IRoute53Service>();

            command = new ListResourceRecordSets
                      {
                          Route53Service = route53ServiceMock.Object
                      };
        }

        [Test]
        public void TestExecute()
        {
            var options = new ListResourceRecordSets.Options
                          {
                              ZoneId = "AABBDDCCDDEE"
                          };

            route53ServiceMock
                .Setup(call => call.ToRegionEndPoint(null))
                .Returns(RegionEndpoint.APNortheast1);
            
            var result = command.Execute(options);

            route53ServiceMock
                .Verify(call => call.ListResourceRecordSets(RegionEndpoint.APNortheast1, "AABBDDCCDDEE"), Times.Once());

            Assert.AreEqual(0, result);
        }
    }
}