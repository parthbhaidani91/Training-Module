using Ctail.Training.Plugins.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Ctail.Training.Plugins.Entities.Rating
{
    /// <summary>
    /// This Plugin will trigger on Update (only on Training Column, because it is updated after the creation via a workflow)
    /// Restrict Duplicate Rating
    /// Rollup the Rating to Training
    /// </summary>
    public class RestrictCreateAndRollup : IPlugin
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
            EntityReference training = null;
            EntityReference attendee = null;
            Entity postImage = null;
            Entity target = null;
            Entity preImage = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Validate the Message and Proceed
                if (config.PluginContext.MessageName.ToLower() == "update" || config.PluginContext.MessageName.ToLower() == "create")
                {
                    //Get the Post Image
                    postImage = config.PluginContext.PostEntityImages["PostImage"];

                    //Get the Target
                    target = config.PluginContext.InputParameters["Target"] as Entity;

                    //Validate the Post Image
                    if (postImage != null)
                    {
                        //Get the Training
                        training = postImage.Attributes.Contains("ctail_training") ? postImage.GetAttributeValue<EntityReference>("ctail_training") : null;
                        //Get the Registrant
                        attendee = postImage.Attributes.Contains("ctail_attendee") ? postImage.GetAttributeValue<EntityReference>("ctail_attendee") : null;

                        //Validate if all the two required attributes are obtained form the PostImage and proceed
                        if (training != null && attendee != null)
                        {
                            //This condition checks whether the record is being activated
                            if ((target.Attributes.Contains("statecode") && target.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                                || !target.Attributes.Contains("statecode"))
                                //Check for Duplicacy of the Rating
                                CheckDuplicacyOfRating(training, attendee, config);

                            //Rollup Average
                            RollupRatingAverage(training, config);
                        }
                        else
                            config.Tracing.Trace($"Returned from: {functionName}, either of the two required attributes is/are not obtained.");
                    }
                    else
                        config.Tracing.Trace($"Returned from: {functionName}, because Post Image is null.");
                }
                else if (config.PluginContext.MessageName.ToLower() == "delete")
                {
                    //Get the Pre Image
                    preImage = config.PluginContext.PreEntityImages["PreImage"];

                    //Validate the Pre Image
                    if (preImage != null)
                    {
                        //Get the Training
                        training = preImage.Attributes.Contains("ctail_training") ? preImage.GetAttributeValue<EntityReference>("ctail_training") : null;

                        //Validate if all the required attributes are obtained form the PreImage and proceed
                        if (training != null)
                        {
                            //Rollup Average
                            RollupRatingAverage(training, config);
                        }
                        else
                            config.Tracing.Trace($"Returned from: {functionName}, the required attributes is/are not obtained.");
                    }
                    else
                        config.Tracing.Trace($"Returned from: {functionName}, because Pre Image is null.");
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
        /// Check if Attendee is trying to rate twice and then throw error
        /// </summary>
        /// <param name="training"></param>
        /// <param name="attendee"></param>
        /// <param name="config"></param>
        private void CheckDuplicacyOfRating(EntityReference training, EntityReference attendee, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "CheckDuplicacyOfRating";
            EntityCollection ratings = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Retrieve Ratings
                ratings = RetrieveRatings(training, attendee, config);

                //Validate
                if (ratings != null && ratings.Entities != null && ratings.Entities.Count > 0)
                    throw new InvalidPluginExecutionException("Looks like you have already provided your rating!");
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
        /// Retrieve Ratings based on the combination of Attendee and Training
        /// </summary>
        /// <param name="training"></param>
        /// <param name="attendee"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private EntityCollection RetrieveRatings(EntityReference training, EntityReference attendee, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveRatings";
            EntityCollection ratings = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='true' distinct='false'>
                                      <entity name='ctail_rating'>
                                        <attribute name='ctail_ratingid' />
                                        <filter type='and'>
                                          <condition attribute='ctail_attendee' operator='eq' value='{attendee.Id}' />
                                          <condition attribute='ctail_training' operator='eq' value='{training.Id}' />
                                          <condition attribute='ctail_ratingid' operator='ne' value='{config.PluginContext.PrimaryEntityId}' />
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                ratings = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
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
            return ratings;
        }

        /// <summary>
        /// Rollup the Rating average to the Training
        /// </summary>
        /// <param name="training"></param>
        /// <param name="config"></param>
        private void RollupRatingAverage(EntityReference training, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RollupRatingAverage";
            #endregion
            try
            {
                string fetchXML = $@"<fetch mapping='logical' distinct='false' no-lock='true' aggregate='true'>
                                      <entity name='ctail_rating'>
                                        <attribute name='ctail_rating' alias='rating_avg' aggregate='avg'/>
                                        <filter type='and'>
                                          <condition attribute='ctail_training' operator='eq' value='{training.Id}' />
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                EntityCollection ratings = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
                config.Tracing.Trace($"{functionName}: After Retrieve");

                Entity trainingEnt = new Entity(training.LogicalName, training.Id);

                foreach (Entity rating in ratings.Entities)
                {
                    config.Tracing.Trace($"{functionName}: Inside For Each");
                    var ratingAverageVar = ((AliasedValue)rating["rating_avg"]).Value;
                    config.Tracing.Trace($"{functionName}: Rating Average: {ratingAverageVar}");

                    trainingEnt.Attributes.Add("ctail_rating", ratingAverageVar);
                    config.Tracing.Trace($"{functionName}: Before Update");
                    config.Service.Update(trainingEnt);
                    config.Tracing.Trace($"{functionName}: After Update");

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
