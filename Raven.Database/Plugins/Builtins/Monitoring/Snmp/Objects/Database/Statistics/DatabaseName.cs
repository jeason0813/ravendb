﻿// -----------------------------------------------------------------------
//  <copyright file="DatabaseOpenedCount.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Pipeline;

using Raven.Abstractions.Data;
using Raven.Database.Server.Tenancy;

namespace Raven.Database.Plugins.Builtins.Monitoring.Snmp.Objects.Database.Statistics
{
	public class DatabaseName : DatabaseScalarObjectBase
	{
		private readonly OctetString name;

		public DatabaseName(string databaseName, DatabasesLandlord landlord, int index)
			: base(databaseName, landlord, "1.5.2.{0}.1.1", index)
		{
			name = new OctetString(databaseName ?? Constants.SystemDatabase);
		}

		protected override ISnmpData GetData(DocumentDatabase database)
		{
			return name;
		}
	}
}