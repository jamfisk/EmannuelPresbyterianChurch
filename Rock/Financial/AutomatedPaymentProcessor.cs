// <copyright>
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
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Financial
{
    /// <summary>
    /// This class handles charging and then storing a payment. Payments must be made through a gateway
    /// supporting automated charging. Payments must be made for an existing person with a saved account.
    /// Use a new instance of this class for every payment.
    /// </summary>
    public class AutomatedPaymentProcessor
    {
        // Constructor params
        private RockContext _rockContext;
        private AutomatedPaymentArgs _automatedPaymentArgs;

        // Declared services
        private PersonAliasService _personAliasService;
        private FinancialGatewayService _financialGatewayService;
        private FinancialAccountService _financialAccountService;
        private FinancialPersonSavedAccountService _financialPersonSavedAccountService;
        private FinancialBatchService _financialBatchService;
        private FinancialTransactionService _financialTransactionService;

        // Loaded entities
        private Person _authorizedPerson;
        private FinancialGateway _financialGateway;
        private GatewayComponent _automatedGatewayComponent;
        private Dictionary<int, FinancialAccount> _financialAccounts;
        private FinancialPersonSavedAccount _financialPersonSavedAccount;
        private ReferencePaymentInfo _referencePaymentInfo;
        private DefinedValueCache _transactionType;
        private DefinedValueCache _financialSource;

        // Results
        private Guid _transactionGuid;
        private FinancialTransaction _financialTransaction;

        /// <summary>
        /// Create a new payment processor to handle a single automated payment. A new RockContext will be created.
        /// </summary>
        /// <param name="automatedPaymentArgs">The arguments describing how toi charge the payment and store the resulting transaction</param>
        public AutomatedPaymentProcessor( AutomatedPaymentArgs automatedPaymentArgs ) : this( automatedPaymentArgs, null )
        {
        }

        /// <summary>
        /// Create a new payment processor to handle a single automated payment.
        /// </summary>
        /// <param name="automatedPaymentArgs">The arguments describing how toi charge the payment and store the resulting transaction</param>
        /// <param name="rockContext">The context to use for loading and saving entities</param>
        public AutomatedPaymentProcessor( AutomatedPaymentArgs automatedPaymentArgs, RockContext rockContext )
        {
            _rockContext = rockContext ?? new RockContext();
            _automatedPaymentArgs = automatedPaymentArgs;

            _personAliasService = new PersonAliasService( rockContext );
            _financialGatewayService = new FinancialGatewayService( rockContext );
            _financialAccountService = new FinancialAccountService( _rockContext );
            _financialPersonSavedAccountService = new FinancialPersonSavedAccountService( rockContext );
            _financialBatchService = new FinancialBatchService( rockContext );
            _financialTransactionService = new FinancialTransactionService( _rockContext );

            _transactionGuid = Guid.NewGuid();
            _financialTransaction = null;
        }

        /// <summary>
        /// Validates that the args do not seem to be a repeat charge on the same person in a short timeframe.
        /// Entities are loaded from supplied IDs where applicable to ensure existance and a valid state.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if charge does not seem repeated. Otherwise a message will be set indicating the problem.</param>
        /// <returns>True if the charge is a repeat. False otherwise.</returns>
        public bool IsRepeatCharge( out string errorMessage )
        {
            errorMessage = string.Empty;

            LoadEntities();

            if ( _automatedPaymentArgs.IgnoreRepeatChargeProtection )
            {
                return false;
            }

            var personAliasIds = _personAliasService.Queryable()
                .AsNoTracking()
                .Where( a => a.Person.GivingId == _authorizedPerson.GivingId )
                .Select( a => a.Id )
                .ToList();

            var minDateTime = RockDateTime.Now.AddMinutes( -5 );
            var repeatTransaction = _financialTransactionService.Queryable()
                .AsNoTracking()
                .Where( t => t.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains( t.AuthorizedPersonAliasId.Value ) )
                .Where( t => t.TransactionDateTime >= minDateTime )
                .FirstOrDefault();

            if ( repeatTransaction != null )
            {
                errorMessage = string.Format( "Found a likely repeat charge. Check transaction id: {0}. Use IgnoreRepeatChargeProtection option to disable this protection.", repeatTransaction.Id );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates the arguments supplied to the constructor. Entities are loaded from supplied IDs where applicable to ensure existance and a valid state.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if arguments are valid. Otherwise a message will be set indicating the problem.</param>
        /// <returns>True if the arguments are valid. False otherwise.</returns>
        public bool AreArgsValid( out string errorMessage )
        {
            errorMessage = string.Empty;

            LoadEntities();

            if ( _authorizedPerson == null )
            {
                errorMessage = "The authorizedPersonAliasId did not resolve to a person";
                return false;
            }

            if ( _financialGateway == null )
            {
                errorMessage = "The gatewayId did not resolve";
                return false;
            }

            if ( !_financialGateway.IsActive )
            {
                errorMessage = "The gateway is not active";
                return false;
            }

            if ( _automatedGatewayComponent as IAutomatedGatewayComponent == null )
            {
                errorMessage = "The gateway failed to produce an automated gateway component";
                return false;
            }

            if ( _automatedPaymentArgs.AutomatedPaymentDetails == null || !_automatedPaymentArgs.AutomatedPaymentDetails.Any() )
            {
                errorMessage = "At least one item is required in the TransactionDetails";
                return false;
            }

            if ( _financialAccounts.Count != _automatedPaymentArgs.AutomatedPaymentDetails.Count )
            {
                errorMessage = "Each detail must reference a unique financial account";
                return false;
            }

            var totalAmount = 0m;

            foreach ( var detail in _automatedPaymentArgs.AutomatedPaymentDetails )
            {
                if ( detail.Amount <= 0m )
                {
                    errorMessage = "The detail amount must be greater than $0";
                    return false;
                }

                var financialAccount = _financialAccounts[detail.AccountId];

                if ( financialAccount == null )
                {
                    errorMessage = string.Format( "The accountId '{0}' did not resolve", detail.AccountId );
                    return false;
                }

                if ( !financialAccount.IsActive )
                {
                    errorMessage = string.Format( "The account '{0}' is not active", detail.AccountId );
                    return false;
                }

                totalAmount += detail.Amount;
            }

            if ( totalAmount < 1m )
            {
                errorMessage = "The total amount must be at least $1";
                return false;
            }

            if ( _financialPersonSavedAccount == null && _automatedPaymentArgs.FinancialPersonSavedAccountId.HasValue )
            {
                errorMessage = string.Format( "The saved account '{0}' does not exist for the person", _automatedPaymentArgs.FinancialPersonSavedAccountId.Value );
                return false;
            }

            if ( _financialPersonSavedAccount == null )
            {
                errorMessage = string.Format( "The given person does not have a saved account" );
                return false;
            }

            if ( _referencePaymentInfo == null )
            {
                errorMessage = string.Format( "The saved account failed to produce reference payment info" );
                return false;
            }

            if ( _transactionType == null )
            {
                errorMessage = string.Format( "The transaction type is invalid" );
                return false;
            }

            if ( _financialSource == null )
            {
                errorMessage = string.Format( "The financial source is invalid" );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the arguments, changes the payment to the gateway, and then stores the resulting transaction in the database.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if arguments are valid and payment succeeds. Otherwise a message will be set indicating the problem.</param>
        /// <returns>The resulting transaction which has been stored in the database</returns>
        public FinancialTransaction ProcessCharge( out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( _financialTransaction != null )
            {
                errorMessage = "A transaction has already been produced";
                return null;
            }

            if ( IsRepeatCharge( out errorMessage ) )
            {
                return null;
            }

            if ( !AreArgsValid( out errorMessage ) )
            {
                return null;
            }

            _referencePaymentInfo.Amount = _automatedPaymentArgs.AutomatedPaymentDetails.Sum( d => d.Amount );
            _referencePaymentInfo.Email = _authorizedPerson.Email;

            _financialTransaction = (_automatedGatewayComponent as IAutomatedGatewayComponent).AutomatedCharge( _financialGateway, _referencePaymentInfo, out errorMessage );

            if ( !string.IsNullOrEmpty( errorMessage ) )
            {
                errorMessage = string.Format( "Error charging: {0}", errorMessage );
                return null;
            }

            if ( _financialTransaction == null )
            {
                errorMessage = "Error charging: transaction was not created";
                return null;
            }

            SaveTransaction();

            return _financialTransaction;
        }

        /// <summary>
        /// Safely load entities that have not yet been assigned a non-null value based on the arguments.
        /// </summary>
        private void LoadEntities()
        {
            if ( _authorizedPerson == null )
            {
                _authorizedPerson = _personAliasService.GetPersonNoTracking( _automatedPaymentArgs.AuthorizedPersonAliasId );
            }

            if ( _financialGateway == null )
            {
                _financialGateway = _financialGatewayService.GetNoTracking( _automatedPaymentArgs.AutomatedGatewayId );
            }

            if ( _financialGateway != null && _automatedGatewayComponent == null )
            {
                _automatedGatewayComponent = _financialGateway.GetGatewayComponent();
            }

            if ( _financialAccounts == null )
            {
                var accountIds = _automatedPaymentArgs.AutomatedPaymentDetails.Select( d => d.AccountId ).ToList();
                _financialAccounts = _financialAccountService.GetByIds( accountIds ).AsNoTracking().ToDictionary( fa => fa.Id, fa => fa );
            }

            if ( _authorizedPerson != null && _financialPersonSavedAccount == null )
            {
                var savedAccountQry = _financialPersonSavedAccountService.GetByPersonId( _authorizedPerson.Id ).AsNoTracking();

                if ( _automatedPaymentArgs.FinancialPersonSavedAccountId.HasValue )
                {
                    var savedAccountId = _automatedPaymentArgs.FinancialPersonSavedAccountId.Value;
                    _financialPersonSavedAccount = savedAccountQry.FirstOrDefault( sa => sa.Id == savedAccountId );
                }
                else
                {
                    _financialPersonSavedAccount = savedAccountQry.FirstOrDefault( sa => sa.IsDefault ) ?? savedAccountQry.FirstOrDefault();
                }
            }

            if ( _financialPersonSavedAccount != null )
            {
                _referencePaymentInfo = _financialPersonSavedAccount.GetReferencePayment();
            }

            if ( _transactionType == null )
            {
                _transactionType = DefinedValueCache.Get( _automatedPaymentArgs.TransactionTypeGuid ?? SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            }

            if ( _financialSource == null )
            {
                _financialSource = DefinedValueCache.Get( _automatedPaymentArgs.FinancialSourceGuid ?? SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE.AsGuid() );
            }
        }

        /// <summary>
        /// Once _financialTransaction is set, this method stores the transaction in the database along with the appropriate details and batch information.
        /// </summary>
        private void SaveTransaction()
        {
            _financialTransaction.Guid = _transactionGuid;
            _financialTransaction.AuthorizedPersonAliasId = _authorizedPerson.PrimaryAliasId;
            _financialTransaction.ShowAsAnonymous = _automatedPaymentArgs.ShowAsAnonymous;
            _financialTransaction.TransactionDateTime = RockDateTime.Now;
            _financialTransaction.FinancialGatewayId = _financialGateway.Id;
            _financialTransaction.TransactionTypeValueId = _transactionType.Id;
            _financialTransaction.Summary = _referencePaymentInfo.Comment1;
            _financialTransaction.SourceTypeValueId = _financialSource.Id;

            if ( _financialTransaction.FinancialPaymentDetail == null )
            {
                _financialTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            _financialTransaction.FinancialPaymentDetail.SetFromPaymentInfo( _referencePaymentInfo, _automatedGatewayComponent, _rockContext );

            foreach ( var detailArgs in _automatedPaymentArgs.AutomatedPaymentDetails )
            {
                var transactionDetail = new FinancialTransactionDetail();
                transactionDetail.Amount = detailArgs.Amount;
                transactionDetail.AccountId = detailArgs.AccountId;

                _financialTransaction.TransactionDetails.Add( transactionDetail );
            }

            var batch = _financialBatchService.Get(
                _automatedPaymentArgs.BatchNamePrefix ?? "Online Giving",
                _referencePaymentInfo.CurrencyTypeValue,
                _referencePaymentInfo.CreditCardTypeValue,
                _financialTransaction.TransactionDateTime.Value,
                _financialGateway.GetBatchTimeOffset() );

            var batchChanges = new History.HistoryChangeList();

            if ( batch.Id == 0 )
            {
                batchChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, "Batch" );
                History.EvaluateChange( batchChanges, "Batch Name", string.Empty, batch.Name );
                History.EvaluateChange( batchChanges, "Status", null, batch.Status );
                History.EvaluateChange( batchChanges, "Start Date/Time", null, batch.BatchStartDateTime );
                History.EvaluateChange( batchChanges, "End Date/Time", null, batch.BatchEndDateTime );
            }

            var newControlAmount = batch.ControlAmount + _financialTransaction.TotalAmount;
            History.EvaluateChange( batchChanges, "Control Amount", batch.ControlAmount.FormatAsCurrency(), newControlAmount.FormatAsCurrency() );
            batch.ControlAmount = newControlAmount;

            _financialTransaction.BatchId = batch.Id;
            _financialTransaction.LoadAttributes( _rockContext );

            batch.Transactions.Add( _financialTransaction );

            _rockContext.SaveChanges();
            _financialTransaction.SaveAttributeValues();

            HistoryService.SaveChanges(
                _rockContext,
                typeof( FinancialBatch ),
                SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                batch.Id,
                batchChanges
            );
        }
    }
}
