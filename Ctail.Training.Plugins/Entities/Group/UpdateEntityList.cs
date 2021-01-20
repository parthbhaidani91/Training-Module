using Ctail.Training.Plugins.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Ctail.Training.Plugins.Entities.Group
{
    public class UpdateEntityList : IPlugin
    {
        #region Unsecure Config
        private string _unsecureConfig { get; set; }

        public UpdateEntityList(string unsecureConfig, string secureConfig)
        {
            if (string.IsNullOrEmpty(unsecureConfig))
                throw new InvalidPluginExecutionException("Plugin Configuration missing.");
            _unsecureConfig = unsecureConfig;
        }
        #endregion

        #region Execute Method
        /// <summary>
        /// Update the Entity List with the updated Filter Definition
        /// Filter Definition is what drives the Group based filtering on Portal
        /// </summary>
        /// <param name="serviceProvider"></param>
        public void Execute(IServiceProvider serviceProvider)
        {
            #region Function Level Variables
            string functionName = "Execute";
            PluginConfig config = null;
            #endregion
            try
            {
                config = new PluginConfig(serviceProvider);

                #region Trace Plugin Related Information
                config.Tracing.Trace($"Depth: {config.PluginContext.Depth}");
                config.Tracing.Trace($"Initiating User Id: {config.PluginContext.InitiatingUserId}");
                config.Tracing.Trace($"User Id: {config.PluginContext.UserId}");
                config.Tracing.Trace($"Message: {config.PluginContext.MessageName}");
                config.Tracing.Trace($"Primary Entity Name: {config.PluginContext.PrimaryEntityName}");
                config.Tracing.Trace($"Primary Entity Id: {config.PluginContext.PrimaryEntityId}");
                #endregion

                #region Initiate Process
                Initiate(config);
                #endregion
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// The main logic begins here
        /// </summary>
        /// <param name="config"></param>
        private void Initiate(PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "Initiate";
            EntityCollection groups = null;
            FilterDefinition filterDefinition = null;
            string filterDefinitionJSON = string.Empty;
            Entity entityList = null;
            string rawFetch = string.Empty;
            List<Condition> conditions = new List<Condition>();
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Retrieve Groups
                groups = RetrieveGroups(config);

                //Retrieve Entity List based on the unsecure config
                entityList = RetrieveEntityList(config);

                //Validate group and proceed
                if (groups != null && groups.Entities.Count > 0)
                {
                    //Get the Filter Definition
                    filterDefinitionJSON = entityList != null && entityList.Attributes.Contains("adx_filter_definition")
                                            ? entityList.GetAttributeValue<string>("adx_filter_definition")
                                            : string.Empty;

                    //Validate filterDefinitionJSON and then only proceed
                    if (!string.IsNullOrEmpty(filterDefinitionJSON))
                    {
                        //Deserialize
                        filterDefinition = filterDefinitionJSON.Deserialize<FilterDefinition>();

                        //Generate Fetch
                        rawFetch = GenerateFetchXML(groups, config);

                        //Generate Conditions
                        conditions = GenerateConditions(groups, config);

                        //Update Entity List
                        UpdateRecord(entityList, rawFetch, conditions, filterDefinition, config);
                    }
                    else
                    {
                        config.Tracing.Trace($"{functionName}: No Filter Definition exist to update.");
                    }
                }
                else
                {
                    UpdateRecord(entityList, string.Empty, null, null, config);
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
        }

        /// <summary>
        /// Retrieve all the Active Groups
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private EntityCollection RetrieveGroups(PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveGroups";
            EntityCollection groups = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='ctail_group'>
                                        <attribute name='ctail_name' />
                                        <attribute name='ctail_groupid' />
                                        <order attribute='ctail_name' descending='false' />
                                        <filter type='and'>
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                groups = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            return groups;
        }

        /// <summary>
        /// Retrieve Entity List based on the Unsecure config
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private Entity RetrieveEntityList(PluginConfig config)
        {
            #region Funtion Level Variables
            string functionName = "RetrieveEntityList";
            Entity entityList = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                ColumnSet columnSet = new ColumnSet();
                columnSet.AddColumn("adx_filter_definition");

                config.Tracing.Trace($"{functionName}: Before Retrieve.");
                entityList = config.Service.Retrieve("adx_entitylist", new Guid(_unsecureConfig), columnSet);
                config.Tracing.Trace($"{functionName}: After Retrieve.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            return entityList;
        }

        /// <summary>
        /// Generate Fetch XML based off of Groups retrieved
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private string GenerateFetchXML(EntityCollection groups, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "GenerateFetchXML";
            string fetchXML = string.Empty;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                fetchXML += $@"<link-entity name='ctail_ctail_group_ctail_training' from='ctail_trainingid' to='ctail_trainingid' visible='false' intersect='true'>
                               <link-entity name='ctail_group' from='ctail_groupid' to='ctail_groupid' alias='ad'>
                               <filter type='or'>";

                foreach (Entity group in groups.Entities)
                {
                    string recordName = group.Attributes.Contains("ctail_name")
                                          ? group.GetAttributeValue<string>("ctail_name")
                                          : group.Id.ToString();
                    fetchXML += $@"<condition attribute='ctail_groupid' operator='eq' uiname='{recordName}' uitype='ctail_group' value='{group.Id}' />";
                }

                fetchXML += $@"</filter>
                               </link-entity>
                               </link-entity>";

                config.Tracing.Trace($"{functionName}: Complete Fetch XML: {fetchXML}");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            return fetchXML;
        }

        /// <summary>
        /// Generate Conditions based on the retrieved Groups
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private List<Condition> GenerateConditions(EntityCollection groups, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "GenerateConditions";
            List<Condition> conditions = new List<Condition>();
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Loop throug groups and generate Condition and add it to the list
                for (int i = 0; i < groups.Entities.Count; i++)
                {
                    Entity group = groups.Entities[i];

                    Condition condition = new Condition();
                    condition.attribute = "ctail_groupid";
                    condition.@operator = "eq";
                    condition.uiname = group.Attributes.Contains("ctail_name")
                                          ? group.GetAttributeValue<string>("ctail_name")
                                          : group.Id.ToString();
                    condition.uitype = "ctail_group";
                    condition.value = group.Id.ToString();
                    condition.AdxId = i.ToString();

                    conditions.Add(condition);
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            return conditions;
        }

        /// <summary>
        /// Update Entity List with the necessary values provided
        /// </summary>
        /// <param name="entityList"></param>
        /// <param name="rawFetch"></param>
        /// <param name="conditions"></param>
        /// <param name="filterDefinition"></param>
        /// <param name="config"></param>
        private void UpdateRecord(Entity entityList, string rawFetch, List<Condition> conditions, FilterDefinition filterDefinition, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "UpdateRecord";
            string filterDefinitionJSON = string.Empty;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Validate the rawFetch and proceed accordingly
                if (!string.IsNullOrEmpty(rawFetch))
                {
                    //Update the Raw Fetch
                    config.Tracing.Trace($"{functionName}: Attempting to replace Raw Fetch");
                    filterDefinition.entity.links[0].AdxRawfetch = rawFetch;
                    config.Tracing.Trace($"{functionName}: Raw Fetch Replaced Successfully");

                    //Update the Conditions
                    config.Tracing.Trace($"{functionName}: Attempting to replace Conditions");
                    filterDefinition.entity.links[0].links[0].filters[0].conditions = conditions;
                    config.Tracing.Trace($"{functionName}: Conitions replaced Successfully");

                    //Serialize filterDefinition
                    config.Tracing.Trace($"{functionName}: Attempting to Serialize");
                    filterDefinitionJSON = filterDefinition.Serialize();
                    config.Tracing.Trace($"{functionName}: Serialization Completed");
                }

                //First Remove the Attribute and then re add with the updated values
                if (entityList.Attributes.Contains("adx_filter_definition"))
                {
                    entityList.Attributes.Remove("adx_filter_definition");
                    entityList.Attributes.Add("adx_filter_definition", filterDefinitionJSON);

                    config.Tracing.Trace($"{functionName}: Before Update");
                    config.Service.Update(entityList);
                    config.Tracing.Trace($"{functionName}: After Update");
                }
                else
                {
                    config.Tracing.Trace($"{functionName}: Filter Definition Doesn't Exists!");
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (InvalidPluginExecutionException ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
        }

        #endregion
    }
}
