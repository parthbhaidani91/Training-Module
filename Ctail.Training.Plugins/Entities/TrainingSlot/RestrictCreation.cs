using Ctail.Training.Plugins.Helper;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Ctail.Training.Plugins.Entities.TrainingSlot
{
    /// <summary>
    /// Restrict Creation of Duplicate Training Slot
    /// </summary>
    public class RestrictCreation : IPlugin
    {
        #region Execute Method
        /// <summary>
        /// This is the entry point of the plugin
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
            DateTime scheduledDate = DateTime.MinValue;
            EntityReference training = null;
            Entity postImage = null;
            EntityCollection trainingSlots = null;
            int? getTimeZoneCode = null;
            DateTime localDateTime = DateTime.MinValue;
            Entity target = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Get the Post Image
                postImage = config.PluginContext.PostEntityImages["PostImage"];
                //Get the Target
                target = config.PluginContext.InputParameters["Target"] as Entity;

                //Return if the record is being deactivated
                if (target.Attributes.Contains("statecode") && target.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                {
                    config.Tracing.Trace($"{functionName}: Returning because the record is being deactivated.");
                    return;
                }

                //Validate the Post Image
                if (postImage != null)
                {
                    //Get the Scheduled Date
                    scheduledDate = postImage.Attributes.Contains("ctail_scheduleddate") ? postImage.GetAttributeValue<DateTime>("ctail_scheduleddate") : scheduledDate;
                    //Get the Training
                    training = postImage.Attributes.Contains("ctail_training") ? postImage.GetAttributeValue<EntityReference>("ctail_training") : null;

                    //Validate if all the two required attributes are obtained form the PostImage and proceed
                    if (scheduledDate != DateTime.MinValue && training != null)
                    {
                        config.Tracing.Trace($"{functionName}: Scheduled Date Before: {scheduledDate}");
                        getTimeZoneCode = RetrieveCurrentUsersSettings(config);
                        localDateTime = RetrieveLocalTimeFromUTCTime(scheduledDate, getTimeZoneCode, config);
                        config.Tracing.Trace($"{functionName}: Scheduled Date After: {localDateTime}");

                        trainingSlots = RetrieveTrainingSlots(localDateTime, training, config);

                        if (trainingSlots != null && trainingSlots.Entities.Count > 0)
                            throw new InvalidPluginExecutionException("Similar Training Slot exists. Please try again with a different Schedule.");
                    }
                    else
                        config.Tracing.Trace($"Returned from: {functionName}, either of the two required attributes is/are not obtained.");
                }
                else
                    config.Tracing.Trace($"Returned from: {functionName}, because Post Image is null.");

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
        /// Retrieve all the Training Slots for the Same Time for the Respective Training
        /// </summary>
        /// <param name="scheduledDate"></param>
        /// <param name="training"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private EntityCollection RetrieveTrainingSlots(DateTime scheduledDate, EntityReference training, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveTrainingSlots";
            EntityCollection trainingSlots = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='ctail_trainingslot'>
                                        <attribute name='ctail_trainingslotid' />
                                        <filter type='and'>
                                          <condition attribute='ctail_scheduleddate' operator='eq' value='{scheduledDate}' />
                                          <condition attribute='ctail_training' operator='eq' value='{training.Id}' />
                                          <condition attribute='ctail_trainingslotid' operator='ne' value='{config.PluginContext.PrimaryEntityId}' />
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                config.Tracing.Trace($"{functionName}: {fetchXML}");

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                trainingSlots = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
                config.Tracing.Trace($"{functionName}: After Retrieve");
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
            return trainingSlots;
        }

        /// <summary>
        /// Retrieve Current Users TimeZone Code
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private int? RetrieveCurrentUsersSettings(PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveCurrentUsersSettings";
            Entity currentUserSettings = null;
            int? timeZoneCode = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                currentUserSettings = config.Service.RetrieveMultiple(
                                        new QueryExpression("usersettings")
                                        {
                                            ColumnSet = new ColumnSet("timezonecode"),
                                            Criteria = new FilterExpression
                                            {
                                                Conditions =
                                                {
                                            new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
                                                }
                                            }
                                        }).Entities[0].ToEntity<Entity>();

                //return time zone code
                timeZoneCode = (int?)currentUserSettings.Attributes["timezonecode"];

                config.Tracing.Trace($"{functionName}: {timeZoneCode}");
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
            return timeZoneCode;
        }

        /// <summary>
        /// Retrieve Local Time from UTC Time
        /// </summary>
        /// <param name="utcTime"></param>
        /// <param name="timeZoneCode"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, int? timeZoneCode, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveLocalTimeFromUTCTime";
            DateTime localTime = DateTime.MinValue;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                if (!timeZoneCode.HasValue)
                    return utcTime;

                LocalTimeFromUtcTimeRequest request = new LocalTimeFromUtcTimeRequest
                {
                    TimeZoneCode = timeZoneCode.Value,
                    UtcTime = utcTime.ToUniversalTime()
                };

                var response = (LocalTimeFromUtcTimeResponse)config.Service.Execute(request);

                localTime = response.LocalTime;

                config.Tracing.Trace($"{functionName}: Local Time: {localTime}");
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
            return localTime;
        }

        #endregion
    }
}
