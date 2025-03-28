/*
 *  Copyright © 2024 Keyfactor
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Jobs
{
    // The Inventory class implementes IAgentJobExtension and is meant to find all of the certificates in a given certificate store on a given server
    // then return those certificates back to Keyfactor for storing in its database.  Private keys will not be passed back to Keyfactor Command 

    public class Inventory : OrchestratorJob<Inventory>, IInventoryJobExtension
    {
        // Job Entry Point; "ProcessJob" is called by the orchestrator once it receives the job request.
        // "config": parameters provided by Command.
        // "submitInventory": The method to call once the job operations are complete.

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            // Configuration StoreProperties Passed from Command for an Inventory Job have the following structure:
            // { ServerUsername: "",
            //   ServerPassword: "",
            //   JobHistoryId: "",
            //   Capability: 
            //   CertificateStoreDetails: {
            //      ClientMachine: "",
            //      StorePath: "",
            //      StorePassword: "",
            //      Properties: { // CertificateStoreDetails.Properties are dynamic and contain the custom store properties we define in the store type
            //      }
            //   }
            // }            

            Initialize(config);
            logger.LogDebug("Begin Inventory...");

            //List<CurrentInventoryItem> is the collection that the interface expects to return from this job.
            // It will contain a collection of certificates found in the store along with other information about those certificates

            // the default response
            JobResult inventoryResult = new()
            {
                Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId
            };

            try
            {
                HPiLOClient APIclient = APIService.GetRequiredService<HPiLOClient>();
                List<CurrentInventoryItem> inventoryItems =
                    APIclient.GetAllCertificates();
                submitInventory.Invoke(inventoryItems);
                inventoryResult.Result = OrchestratorJobStatusJobResult.Success;
                inventoryResult.FailureMessage = $"Successfully inventoried {inventoryItems.Count} certificates";
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred during the Inventory job: {ex.Message}";
                logger.LogError(ex, errorMessage);
                inventoryResult.FailureMessage = errorMessage;
            }

            return inventoryResult;
        }
    }
}