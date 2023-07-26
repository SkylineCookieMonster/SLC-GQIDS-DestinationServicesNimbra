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

26/07/2023	1.0.0.1		MSA, Skyline	Initial version
****************************************************************************
*/

namespace GetServicesNimbra_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.CommunityLibrary.Netinsight.Nimbra;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "getServicesNimbra")]
	public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{
		private readonly GQIIntArgument _argument = new GQIIntArgument("Customer ID") { IsRequired = false };
		private int? _customerId = null;
		private IDms _dms;
		private IManagerService[] _services;
		private INimbraManager _manager;
		private int limit = 30;
		private int leftOff;

		public INimbraManager Manager
		{
			get
			{
				if (_manager == null)
				{
					_manager = _dms.GetNimbraManager();
				}

				return _manager;
			}
		}

		public IManagerService[] Services
		{
			get
			{
				if (_services == null)
				{
					_services = Manager.Services.ToArray();
				}

				return _services;
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("ID"),
				new GQIStringColumn("Source Node"),
				new GQIStringColumn("Name"),
				new GQIStringColumn("Oper Status"),
				new GQIDoubleColumn("Reserved Cap (Mbps)"),
				new GQIStringColumn("Destination Names"),
				new GQIStringColumn("Source Location"),
				new GQIStringColumn("Destination Location"),
				new GQIDoubleColumn("Requested Capacity"),
				new GQIStringColumn("Type"),
				new GQIStringColumn("Continent"),
			};
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _argument };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			_customerId = args.GetArgumentValue(_argument);
			return default;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();
			for (int i = leftOff; i < Services.Length; i++)
			{
				var service = Services[i];
				leftOff++;
				if (_customerId.HasValue && service.CustomerId != _customerId.Value)
				{
					continue;
				}

				string continent = string.Empty;
				if (Manager.Nodes.ContainsKey(service.SourceNodeElementId))
				{
					continent = Manager.Nodes[service.SourceNodeElementId].Continent;
				}

				var newRow = new GQIRow(
					new[]
					{
						new GQICell { Value = service.Key },
						new GQICell { Value = service.ServiceSourceNode },
						new GQICell { Value = service.Name },
						new GQICell { Value = service.OperationalState.ToString() },
						new GQICell { Value = service.ReservedCapacityInMbps },
						new GQICell { Value = string.Join(", ",service.Destinations.Select(d => $"{d.Node.Name}")) },
						new GQICell { Value = service.SourceLocation },
						new GQICell { Value = string.Join(", ",service.DestinationLocations) },
						new GQICell { Value = service.RequestedCapacityInMbps },
						new GQICell { Value = service.ServiceType.ToString() },
						new GQICell { Value = continent },
					});

				rows.Add(newRow);

				if (rows.Count > limit)
				{
					break;
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = leftOff < Services.Length,
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
	}
}