﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using Rock.SystemGuid;
    
    /// <summary>
    ///
    /// </summary>
    public partial class Stripe : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue( DefinedType.FINANCIAL_CURRENCY_TYPE, "Apple Pay", "Apple Pay", DefinedValue.CURRENCY_TYPE_APPLE_PAY, true, 4 );
            RockMigrationHelper.AddDefinedValue( DefinedType.FINANCIAL_CURRENCY_TYPE, "Android Pay", "Android Pay", DefinedValue.CURRENCY_TYPE_ANDROID_PAY, true, 5 );

            AddColumn( "dbo.FinancialTransactionDetail", "FeeAmount", c => c.Decimal( nullable: false, precision: 18, scale: 2 ) );

            AddColumn( "dbo.FinancialPersonSavedAccount", "GatewayPersonIdentifier", c => c.String( maxLength: 50 ) );
            AddColumn( "dbo.FinancialPersonSavedAccount", "IsSystem", c => c.Boolean( nullable: false ) );
            AddColumn( "dbo.FinancialPersonSavedAccount", "IsDefault", c => c.Boolean( nullable: false ) );
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropColumn( "dbo.FinancialPersonSavedAccount", "IsDefault" );
            DropColumn( "dbo.FinancialPersonSavedAccount", "IsSystem" );
            DropColumn( "dbo.FinancialPersonSavedAccount", "GatewayPersonIdentifier" );

            DropColumn( "dbo.FinancialTransactionDetail", "FeeAmount" );

            RockMigrationHelper.DeleteDefinedValue( DefinedValue.CURRENCY_TYPE_ANDROID_PAY );
            RockMigrationHelper.DeleteDefinedValue( DefinedValue.CURRENCY_TYPE_APPLE_PAY );
        }
    }
}