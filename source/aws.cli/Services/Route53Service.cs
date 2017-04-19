﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Aws.Interfaces.Services;
using Sugar.Net;

namespace Aws.Services
{
    /// <summary>
    /// Service to wrap call to the Amazon Route 53 API.
    /// </summary>
    public class Route53Service : IRoute53Service
    {
        #region Depedencies

        /// <summary>
        /// Gets or sets the HTTP service.
        /// </summary>
        /// <value>
        /// The HTTP service.
        /// </value>
        public HttpService HttpService { get; set; }

        #endregion

        private AmazonRoute53Client client;

        private AmazonRoute53Client Client
        {
            get
            {
                if (client != null)
                {
                    return client;
                }

                RegionEndpoint region = null;

                // Region override
                var regionName = Sugar.Command.Parameters.Current.AsString("region", null);
                if (!string.IsNullOrEmpty(regionName))
                {
                    region = RegionEndpoint.GetBySystemName(regionName);
                }
                
                // Profile
                var profileName = Sugar.Command.Parameters.Current.AsString("profile", null);
                if (string.IsNullOrEmpty(profileName))
                {
                    client = region == null ? new AmazonRoute53Client() : new AmazonRoute53Client(region);
                }
                else
                {
                    var profilesLocation = Sugar.Command.Parameters.Current.AsString("profiles-location", null);
                    if (string.IsNullOrEmpty(profilesLocation))
                    {
                        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                        profilesLocation = Path.Combine(userFolder, @".aws\config");
                    }

                    AWSCredentials credentials;

                    var chain = new CredentialProfileStoreChain(profilesLocation);

                    if (!chain.TryGetAWSCredentials(profileName, out credentials))
                    {
                        throw new AmazonClientException("Unable to initialise AWS credentials from profile name");
                    }

                    client = region == null
                        ? new AmazonRoute53Client(credentials)
                        : new AmazonRoute53Client(credentials, region);
                }

                return client;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Route53Service"/> class.
        /// </summary>
        public Route53Service()
        {
            HttpService = new HttpService();
        }
        
        /// <summary>
        /// Gets the meta instance metadata.
        /// </summary>
        /// <param name="key">The key (e.g. public-ipv4.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Unable to determine meta data from: http://169.254.169.254/latest/meta-data/ +
        ///                                         key</exception>
        public string GetMetaInstanceMetadata(string key)
        {
            // This only works if you run this utility on an EC2 instance
            var html = HttpService.Get("http://169.254.169.254/latest/meta-data/" + key, "");

            if (html.Success)
            {
                return html.ToString();
            }

            throw new ArgumentException("Unable to determine meta data from: http://169.254.169.254/latest/meta-data/" + key);
        }

        /// <summary>
        /// Gets the public ip address from the instace meta data HTTP API.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ApplicationException">Unable to determine public IP address from: http://169.254.169.254/latest/meta-data/public-ipv4</exception>
        public string GetPublicIpAddress()
        {
            return GetMetaInstanceMetadata("public-ipv4");
        }

        /// <summary>
        /// Gets the local ip address.
        /// </summary>
        /// <returns></returns>
        public string GetLocalIpAddress()
        {
            return GetMetaInstanceMetadata("local-ipv4");
        }

        /// <summary>
        /// Lists the hosted zones.
        /// </summary>
        /// <returns></returns>
        public List<HostedZone> ListHostedZones()
        {
            var request = new ListHostedZonesRequest();

            var response = Client.ListHostedZones(request);

            return response.HostedZones;
        }

        /// <summary>
        /// Gets the zone.
        /// </summary>
        /// <param name="domainName">Name of the domain.</param>
        /// <returns></returns>
        public HostedZone GetZone(string domainName)
        {
            var request = new ListHostedZonesRequest();
            
            var response = Client.ListHostedZones(request);

            return response.HostedZones
                           .FirstOrDefault(z => z.Name.StartsWith(domainName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Lists the resource record sets in the specified hosted zone.
        /// </summary>
        /// <param name="hostedZoneId">The hosted zone identifier.</param>
        /// <returns></returns>
        public List<ResourceRecordSet> ListResourceRecordSets(string hostedZoneId)
        {
            var request = new ListResourceRecordSetsRequest
                          {
                              HostedZoneId = hostedZoneId
                          };
            
            var response = Client.ListResourceRecordSets(request);

            return response.ResourceRecordSets;
        }

        /// <summary>
        /// Creates the resource record set.
        /// </summary>
        /// <param name="hostedZoneId">The hosted zone identifier.</param>
        /// <param name="newRecord">The new record.</param>
        /// <returns></returns>
        public ChangeInfo CreateResourceRecordSet(string hostedZoneId, ResourceRecordSet newRecord)
        {
            var changes = new List<Change>
                          {
                              new Change
                              {
                                  Action = ChangeAction.CREATE,
                                  ResourceRecordSet = newRecord
                              }
                          };

            return SubmitChangeResourceRecordSets(hostedZoneId, changes);
        }

        /// <summary>
        /// Replaces the resource record set.
        /// </summary>
        /// <param name="hostedZoneId">The hosted zone identifier.</param>
        /// <param name="oldRecord">The old record.</param>
        /// <param name="newRecord">The new record.</param>
        /// <returns></returns>
        public ChangeInfo ReplaceResourceRecordSet(string hostedZoneId, ResourceRecordSet oldRecord, ResourceRecordSet newRecord)
        {
            var changes = new List<Change>
                          {
                              new Change
                              {
                                  Action = ChangeAction.DELETE,
                                  ResourceRecordSet = oldRecord
                              },
                              new Change
                              {
                                  Action = ChangeAction.CREATE,
                                  ResourceRecordSet = newRecord
                              }
                          };

            return SubmitChangeResourceRecordSets(hostedZoneId, changes);
        }

        /// <summary>
        /// Submits the change request.
        /// </summary>
        /// <param name="hostedZoneId">The hosted zone identifier.</param>
        /// <param name="changes">The changes.</param>
        /// <returns></returns>
        private ChangeInfo SubmitChangeResourceRecordSets(string hostedZoneId, List<Change> changes)
        {
            var request = new ChangeResourceRecordSetsRequest
                          {
                              HostedZoneId = hostedZoneId,
                              ChangeBatch = new ChangeBatch {Changes = changes}
                          };
            
            var response = Client.ChangeResourceRecordSets(request);

            return response.ChangeInfo;
        }
    }
}
