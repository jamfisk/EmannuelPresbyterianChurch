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
using System;
using System.Collections.Generic;

namespace Rock.Financial
{
    /// <summary>
    /// For payments made through a REST endpoint or other automated means, this class serves as the options for the transaction and payment.
    /// </summary>
    public class AutomatedPaymentArgs
    {
        /// <summary>
        /// The alias id of the person associated with this financial transaction
        /// </summary>
        public int AuthorizedPersonAliasId { get; set; }

        /// <summary>
        /// The id of the gateway to use when processing the transaction. Must be a gateway that supports automated processing.
        /// </summary>
        public int AutomatedGatewayId { get; set; }

        /// <summary>
        /// The details of the transaction. Details include financial account id and corresponding amount
        /// </summary>
        public ICollection<AutomatedPaymentDetailArgs> AutomatedPaymentDetails { get; set; }

        /// <summary>
        /// The saved account id associated with the given person. If null, the person's default payment method will be applied instead
        /// </summary>
        public int? FinancialPersonSavedAccountId { get; set; }

        /// <summary>
        /// Is this transaction to be shown as anonymous
        /// </summary>
        public bool ShowAsAnonymous { get; set; }

        /// <summary>
        /// What type of transaction should be produced. If null, defaults as contribution.
        /// </summary>
        public Guid? TransactionTypeGuid { get; set; }

        /// <summary>
        /// Where did the payment originate. If null, defaults as web.
        /// </summary>
        public Guid? FinancialSourceGuid { get; set; }

        /// <summary>
        /// The batch prefix name to use when creating a new batch. If null, defaults to "Online Giving".
        /// </summary>
        public string BatchNamePrefix { get; set; }

        /// <summary>
        /// If true, the payment will be charged even if there is a similar transaction for the same person within a short time period.
        /// Otherwise, the payment will not be charged if a smilar transaction within a short time period exists.
        /// </summary>
        public bool IgnoreRepeatChargeProtection { get; set; }

        public class AutomatedPaymentDetailArgs
        {
            /// <summary>
            /// Gets or sets the AccountId of the <see cref="Rock.Model.FinancialAccount"/>/account that that the transaction detail <see cref="Amount"/> should be directed toward.
            /// </summary>
            /// <value>
            /// A <see cref="System.Int32"/> representing the AccountId of the <see cref="Rock.Model.FinancialAccount"/>/account that this transaction detail is directed toward.
            /// </value>
            public int AccountId { get; set; }

            /// <summary>
            /// Gets or sets the purchase/gift amount.
            /// </summary>
            /// <value>
            /// A <see cref="System.Decimal"/> representing the purchase/gift amount.
            /// </value>
            public decimal Amount { get; set; }
        }
    }
}
