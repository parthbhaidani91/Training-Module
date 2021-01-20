using Ctail.Training.Plugins.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Ctail.Training.Plugins.Entities.Registration
{
    /// <summary>
    /// Trigger the Plugin on Create and Update (only of the Training, because it set after the record is created via a workflow)
    /// Restrict Registration Creation when the Duplicate Registration is Created
    /// Restrict Registration Creation when the Available Seats is 0
    /// Rollup the Registration Count to the Training Slots
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
            EntityReference trainingSlot = null;
            EntityReference training = null;
            EntityReference registrant = null;
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
                        //Get the Training Slot
                        trainingSlot = postImage.Attributes.Contains("ctail_trainingslot") ? postImage.GetAttributeValue<EntityReference>("ctail_trainingslot") : null;
                        //Get the Training
                        training = postImage.Attributes.Contains("ctail_training") ? postImage.GetAttributeValue<EntityReference>("ctail_training") : null;
                        //Get the Registrant
                        registrant = postImage.Attributes.Contains("ctail_registrant") ? postImage.GetAttributeValue<EntityReference>("ctail_registrant") : null;

                        //Validate if all the three required attributes are obtained form the PostImage and proceed
                        if (trainingSlot != null && training != null && registrant != null)
                        {
                            //This condition checks whether the record is being activated
                            if ((target.Attributes.Contains("statecode") && target.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                                || !target.Attributes.Contains("statecode"))
                            {
                                //Validate Email Address
                                ValidateEmailAddressOfRegistrant(registrant, config);

                                //Validate Duplicacy
                                CheckDuplicacyOfRegistration(trainingSlot, training, registrant, config);
                            }

                            //Rollup
                            RollupRegistrationCount(trainingSlot, config);
                        }
                        else
                            config.Tracing.Trace($"Returned from: {functionName}, either of the three required attributes is/are not obtained.");
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
                        trainingSlot = preImage.Attributes.Contains("ctail_trainingslot") ? preImage.GetAttributeValue<EntityReference>("ctail_trainingslot") : null;

                        //Validate if all the required attributes are obtained form the PreImage and proceed
                        if (trainingSlot != null)
                        {
                            //Rollup Average
                            RollupRegistrationCount(trainingSlot, config);
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
        /// Validate Email Address of the Registrant using the 3rd Party API
        /// </summary>
        /// <param name="registrant"></param>
        /// <param name="config"></param>
        private void ValidateEmailAddressOfRegistrant(EntityReference registrant, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "ValidateEmailAddressOfRegistrant";
            string emailAddress = string.Empty;
            string response = string.Empty;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Get the Email Address
                emailAddress = RetrieveRegistrantEmailAddress(registrant, config);
                emailAddress = emailAddress.Trim();

                //Validate against the 3rd party system by sending the request
                response = SendRequest(emailAddress, config);
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
        /// Retrieve the Registrant's Email Address
        /// </summary>
        /// <param name="registrant"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private string RetrieveRegistrantEmailAddress(EntityReference registrant, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveRegistrantEmailAddress";
            string emailAddress = string.Empty;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                ColumnSet columnSet = new ColumnSet();
                columnSet.AddColumn("emailaddress1");
                Entity registrantEnt = config.Service.Retrieve(registrant.LogicalName, registrant.Id, columnSet);

                emailAddress = registrantEnt.Attributes.Contains("emailaddress1")
                                ? registrantEnt.GetAttributeValue<string>("emailaddress1")
                                : "abc@xyz.com";
                config.Tracing.Trace($"{functionName}: Email Address: {emailAddress}");
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
            return emailAddress;
        }

        /// <summary>
        /// Send the Request to the 3rd Party API and get the Response
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private string SendRequest(string emailAddress, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "SendRequest";
            string url = @"http://mbshandson.azure-api.net/test?email=";
            string response = string.Empty;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                Uri uri = new Uri($"{url}{emailAddress}");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                request.ContentLength = 0;

                using (WebResponse webResponse = request.GetResponse())
                {
                    // Get the stream associated with the response.
                    Stream receiveStream = webResponse.GetResponseStream();
                    // Pipes the stream to a higher level stream reader with the required encoding format. 
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                    // Read the content
                    response = readStream.ReadToEnd();
                    // Clean up the streams
                    readStream.Close();
                    receiveStream.Close();
                }
                config.Tracing.Trace($"{functionName}: Response: {response}");
            }
            catch (WebException ex)
            {
                // Write out the WebException message.  
                config.Tracing.Trace(ex.ToString());
                // Get the WebException status code.  
                WebExceptionStatus status = ex.Status;
                // If status is WebExceptionStatus.ProtocolError,
                //   there has been a protocol error and a WebResponse
                //   should exist. Display the protocol error.  
                if (status == WebExceptionStatus.ProtocolError)
                {
                    config.Tracing.Trace("The server returned protocol error ");
                    // Get HttpWebResponse so that you can check the HTTP status code.  
                    HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;
                    config.Tracing.Trace($"{(int)httpResponse.StatusCode} - {httpResponse.StatusCode}");
                }
                throw new InvalidPluginExecutionException("A WebException has been caught.");
            }
            catch (Exception ex)
            {
                config.Tracing.Trace($"Exited from {functionName}");
                throw new InvalidPluginExecutionException(ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Check if Registrant is trying to register twice and then throw error
        /// </summary>
        /// <param name="trainingSlot"></param>
        /// <param name="training"></param>
        /// <param name="registrant"></param>
        /// <param name="config"></param>
        private void CheckDuplicacyOfRegistration(EntityReference trainingSlot, EntityReference training, EntityReference registrant, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "CheckDuplicacyOfRegistration";
            EntityCollection registrations = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                //Retrieve Registrations
                registrations = RetrieveRegistrations(trainingSlot, training, registrant, config);

                //Validate
                if (registrations != null && registrations.Entities != null && registrations.Entities.Count > 0)
                    throw new InvalidPluginExecutionException("You are already registered for the training!");
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
        /// Retrieve the Registrations based on the combination of
        /// Training Slot, Training and Registrations
        /// Note: Since, this plugin is being triggered on Update, we need to add the condition of excluding self record
        /// </summary>
        /// <param name="trainingSlot"></param>
        /// <param name="training"></param>
        /// <param name="registrant"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private EntityCollection RetrieveRegistrations(EntityReference trainingSlot, EntityReference training, EntityReference registrant, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveRegistrations";
            EntityCollection registrations = null;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
                                      <entity name='ctail_trainingslot'>
                                        <attribute name='ctail_trainingslotid' />
                                        <filter type='and'>
                                          <condition attribute='ctail_trainingslotid' operator='eq' value='{trainingSlot.Id}' />
                                          <condition attribute='ctail_training' operator='eq' value='{training.Id}' />
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                        <link-entity name='ctail_registration' from='ctail_trainingslot' to='ctail_trainingslotid' link-type='inner' alias='ac'>
                                          <filter type='and'>
                                            <condition attribute='ctail_registrationid' operator='ne' value='{config.PluginContext.PrimaryEntityId}' />
                                            <condition attribute='ctail_registrant' operator='eq' value='{registrant.Id}' />
                                            <condition attribute='statecode' operator='eq' value='0' />
                                          </filter>
                                        </link-entity>
                                      </entity>
                                    </fetch>";

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                registrations = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
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
            return registrations;
        }

        /// <summary>
        /// Rollup the Registrations count to the Training Slot
        /// </summary>
        /// <param name="trainingSlot"></param>
        /// <param name="config"></param>
        private void RollupRegistrationCount(EntityReference trainingSlot, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RollupRegistrationCount";
            EntityCollection registrations = null;
            int count = 0;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                string fetchXML = $@"<fetch mapping='logical' distinct='false' no-lock='true' aggregate='true'>
                                      <entity name='ctail_registration'>
                                        <attribute name='ctail_registrationid' alias='registration_count' aggregate='count'/>
                                        <filter type='and'>
                                          <condition attribute='ctail_trainingslot' operator='eq' value='{trainingSlot.Id}' />
                                          <condition attribute='statecode' operator='eq' value='0' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                registrations = config.Service.RetrieveMultiple(new FetchExpression(fetchXML));
                config.Tracing.Trace($"{functionName}: After Retrieve");

                foreach (Entity registration in registrations.Entities)
                {
                    count = (int)((AliasedValue)registration["registration_count"]).Value;
                    config.Tracing.Trace($"{functionName}: Count: {count}");
                }

                //Get the Total Seats count
                int totalSeats = RetrieveTotalSeats(trainingSlot, config);

                //If such scenario arises, it means that it is time to throw error
                if (count > totalSeats)
                {
                    throw new InvalidPluginExecutionException("Looks like it is a houseful. Try again for a different slot!");
                }

                Entity trainingSlotEnt = new Entity(trainingSlot.LogicalName, trainingSlot.Id);
                trainingSlotEnt.Attributes.Add("ctail_filledupseats", count);

                config.Tracing.Trace($"{functionName}: Before Update.");
                config.Service.Update(trainingSlotEnt);
                config.Tracing.Trace($"{functionName}: After Update.");
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
        /// Retrieve Total Seats to validate against the new count and then restrict if it is exceeding
        /// </summary>
        /// <param name="trainingSlot"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private int RetrieveTotalSeats(EntityReference trainingSlot, PluginConfig config)
        {
            #region Function Level Variables
            string functionName = "RetrieveTotalSeats";
            int totalSeats = 0;
            #endregion
            try
            {
                config.Tracing.Trace($"Inside {functionName}");

                ColumnSet columnSet = new ColumnSet();
                columnSet.AddColumn("ctail_totalseats");

                config.Tracing.Trace($"{functionName}: Before Retrieve");
                Entity trainingSlotEnt = config.Service.Retrieve(trainingSlot.LogicalName, trainingSlot.Id, columnSet);
                config.Tracing.Trace($"{functionName}: After Retrieve");

                if (trainingSlotEnt != null && trainingSlotEnt.Attributes.Contains("ctail_totalseats"))
                {
                    totalSeats = trainingSlotEnt.GetAttributeValue<int>("ctail_totalseats");
                    config.Tracing.Trace($"{functionName}: Total Seats: {totalSeats}");
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
            return totalSeats;
        }

        #endregion
    }
}
