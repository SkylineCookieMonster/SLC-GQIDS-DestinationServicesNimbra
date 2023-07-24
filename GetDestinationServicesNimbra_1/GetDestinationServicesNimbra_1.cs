/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

05-07-2023	1.0.0.1		MichielSA, Skyline	Initial version
****************************************************************************
*/

namespace GetDestinationServicesNimbra_1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.CommunityLibrary.Netinsight.Nimbra;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;

    [GQIMetaData(Name = "getDestinationServicesNimbra")]
    public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
    {
        private readonly GQIStringArgument _argument = new GQIStringArgument("Purpose Filter") { IsRequired = true };
        private string _purposeFilter = string.Empty;
        private IDms _dms;

        public IEnumerable<MergedTables> NimbraDestinations
        {
            get
            {
                var nodes = _dms.GetNimbraNodes().Where(e => e.Element.State == Skyline.DataMiner.Core.DataMinerSystem.Common.ElementState.Active).ToDictionary(n => n.Element.DmsElementId, n => n);

                List<MergedTables> destinationList = new List<MergedTables>();

                foreach (var node in nodes.Values)
                {
                    foreach (var service in node.Services)
                    {
                        foreach (var destination in service.Destinations)
                        {
                            MergedTables mergedTables = new MergedTables
                            {
                                Id = destination.Key,
                                SrcNodeName = service.SrcNodeName,
                                SrcTtpPurpose = service.TtpPurpose,
                                SrcDsti = service.SrcDsti,
                                SrcCustomerId = service.CustomerId,
                                DstOperStatus = destination.DstOperStatus,
                                DstDsti = destination.DstDsti,
                                DstName = destination.DstName,
                                DefaultChannel = destination.DefaultChannel,
                                ProtectionChannel = destination.ProtectionChannel,
                                DefaultSrcRouteName = destination.DefaultSrcRouteName,
                                ProtectionSrcRouteName = destination.ProtectionSrcRouteName,
                            };

                            destinationList.Add(mergedTables);
                        }
                    }
                }

                return destinationList;
            }
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
            new GQIStringColumn("ID"),
            new GQIStringColumn("Src Node"), // Source Node
            new GQIStringColumn("Src TTP Purpose"), // Name
            new GQIStringColumn("Src DSTI"), // Source DSTI
            new GQIIntColumn("Srcs Customer ID"), // Customer ID
            new GQIStringColumn("Oper Status"), // Oper Status
            new GQIStringColumn("Dest DSTI"),
            new GQIStringColumn("Dest Name"), // Destination Name
            new GQIBooleanColumn("Default Channel"),
            new GQIBooleanColumn("Protection Channel"),
            new GQIStringColumn("Default Src Route Name"),
            new GQIStringColumn("Protection Src Route Name"),
            };
        }

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { _argument };
        }

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _purposeFilter = args.GetArgumentValue(_argument);
            return default;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var nimbraDestinations = NimbraDestinations;

            var rows = new List<GQIRow>();

            foreach (var destination in nimbraDestinations)
            {
                var newRow = new GQIRow(
                    new[]
                    {
                        new GQICell { Value = destination.Id },
                        new GQICell { Value = destination.SrcNodeName },
                        new GQICell { Value = destination.SrcTtpPurpose },
                        new GQICell { Value = destination.SrcDsti },
                        new GQICell { Value = destination.SrcCustomerId },
                        new GQICell { Value = destination.DstOperStatus.ToString() },
                        new GQICell { Value = destination.DstDsti },
                        new GQICell { Value = destination.DstName },
                        new GQICell { Value = destination.DefaultChannel },
                        new GQICell { Value = destination.ProtectionChannel },
                        new GQICell { Value = destination.DefaultSrcRouteName },
                        new GQICell { Value = destination.ProtectionSrcRouteName },
                    });

                rows.Add(newRow);
            }

            var filteredRows = rows.Where(row => (string)row.Cells[2].Value == _purposeFilter).ToArray();

            return new GQIPage(filteredRows)
            {
                HasNextPage = false,
            };
        }

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _dms = DmsFactory.CreateDms(new GqiConnection(args.DMS));
            return new OnInitOutputArgs();
        }

        public class GqiConnection : ICommunication
        {
            private readonly GQIDMS _gqiDms;

            public GqiConnection(GQIDMS gqiDms)
            {
                _gqiDms = gqiDms ?? throw new ArgumentNullException(nameof(gqiDms));
            }

            public DMSMessage[] SendMessage(DMSMessage message)
            {
                return _gqiDms.SendMessages(message);
            }

            public DMSMessage SendSingleResponseMessage(DMSMessage message)
            {
                return _gqiDms.SendMessage(message);
            }

            public DMSMessage SendSingleRawResponseMessage(DMSMessage message)
            {
                return _gqiDms.SendMessage(message);
            }

            public void AddSubscriptionHandler(NewMessageEventHandler handler)
            {
                throw new NotImplementedException();
            }

            public void AddSubscriptions(NewMessageEventHandler handler, string handleGuid, SubscriptionFilter[] subscriptions)
            {
                throw new NotImplementedException();
            }

            public void ClearSubscriptionHandler(NewMessageEventHandler handler)
            {
                throw new NotImplementedException();
            }

            public void ClearSubscriptions(NewMessageEventHandler handler, string handleGuid, bool replaceWithEmpty = false)
            {
                throw new NotImplementedException();
            }
        }

        public class MergedTables
        {
            public string Id { get; set; }

            public string SrcNodeName { get; set; }

            public string SrcTtpPurpose { get; set; }

            public string SrcDsti { get; set; }

            public int SrcCustomerId { get; set; }

            public DstOperationStatus DstOperStatus { get; set; }

            public string DstDsti { get; set; }

            public string DstName { get; set; }

            public bool DefaultChannel { get; set; }

            public bool ProtectionChannel { get; set; }

            public string DefaultSrcRouteName { get; set; }

            public string ProtectionSrcRouteName { get; set;}
        }
    }
}