﻿using System.Collections.Generic;
using Amazon;
using Amazon.Route53.Model;
using Aws.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace Aws.Commands
{
    [TestFixture]
    public class ListHostedZonesTest
    {
        private ListHostedZones command;

        private Mock<IRoute53Service> route53ServiceMock;

        [SetUp]
        public void Setup()
        {
            route53ServiceMock = new Mock<IRoute53Service>();

            command = new ListHostedZones
                      {
                          Route53Service = route53ServiceMock.Object
                      };
        }

        [Test]
        public void TestListHostedZones()
        {
            var options = new ListHostedZones.Options
                          {
                              Region = "eu-west-1"
                          };

            route53ServiceMock
                .Setup(call => call.ToRegionEndPoint("eu-west-1"))
                .Returns(RegionEndpoint.EUWest1);

            route53ServiceMock
                .Setup(call => call.ListHostedZones(RegionEndpoint.EUWest1))
                .Returns(new List<HostedZone>());

            var result = command.Execute(options);

            Assert.AreEqual(0, result);
        }
    }
}