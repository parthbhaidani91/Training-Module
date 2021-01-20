using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Ctail.Training.Plugins.Helper
{
    /// <summary>
    /// It encapsulates the basic objects required for a plugin.
    /// </summary>
    public class PluginConfig
    {
        #region Members
        /// <summary>
        /// Tracing object.
        /// </summary>
        private ITracingService _tracing;
        public ITracingService Tracing
        {
            get
            {
                try
                {
                    if (_tracing == null) _tracing = (ITracingService)GetServices(typeof(ITracingService));
                }
                catch (InvalidPluginExecutionException ex) { throw new InvalidPluginExecutionException(ex.Message); }
                catch (Exception ex) { throw new InvalidPluginExecutionException(ex.Message); }

                return _tracing;
            }
        }

        /// <summary>
        /// Plugin Execution context object.
        /// </summary>
        private IPluginExecutionContext _pluginContext;
        public IPluginExecutionContext PluginContext
        {
            get
            {
                try
                {
                    if (_pluginContext == null) _pluginContext = (IPluginExecutionContext)GetServices(typeof(IPluginExecutionContext));
                }
                catch (InvalidPluginExecutionException ex) { throw new InvalidPluginExecutionException(ex.Message); }
                catch (Exception ex) { throw new InvalidPluginExecutionException(ex.Message); }

                return _pluginContext;
            }
        }

        /// <summary>
        /// Service Factory object.
        /// </summary>
        private IOrganizationServiceFactory _serviceFactory;
        public IOrganizationServiceFactory ServiceFactory
        {
            get
            {
                try
                {
                    if (_serviceFactory == null) _serviceFactory = (IOrganizationServiceFactory)GetServices(typeof(IOrganizationServiceFactory));
                }
                catch (InvalidPluginExecutionException ex) { throw new InvalidPluginExecutionException(ex.Message); }
                catch (Exception ex) { throw new InvalidPluginExecutionException(ex.Message); }

                return _serviceFactory;
            }
        }

        /// <summary>
        /// Organization Service object.
        /// </summary>
        private IOrganizationService _service;
        public IOrganizationService Service
        {
            get
            {
                try
                {
                    if (_service == null) _service = ServiceFactory.CreateOrganizationService(PluginContext.UserId);
                }
                catch (InvalidPluginExecutionException ex) { throw new InvalidPluginExecutionException(ex.Message); }
                catch (Exception ex) { throw new InvalidPluginExecutionException(ex.Message); }

                return _service;
            }
        }

        /// <summary>
        /// Organization Service Context object.
        /// </summary>
        private OrganizationServiceContext _context;
        public OrganizationServiceContext Context
        {
            get
            {
                try
                {
                    if (_context == null) _context = new OrganizationServiceContext(Service);
                }
                catch (InvalidPluginExecutionException ex) { throw new InvalidPluginExecutionException(ex.Message); }
                catch (Exception ex) { throw new InvalidPluginExecutionException(ex.Message); }

                return _context;
            }
        }

        /// <summary>
        /// Service provider of plugin.
        /// </summary>
        private IServiceProvider _serviceProvider;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for Plugin Config
        /// </summary>
        /// <param name="pServiceProvider">Object of Service Provider Interface</param>
        public PluginConfig(IServiceProvider pServiceProvider)
        {
            _serviceProvider = pServiceProvider;
        }
        #endregion

        #region Methods
        /// <summary>
        /// It will return the requested service.
        /// </summary>
        /// <param name="pType">Type of service to be returned.</param>
        /// <returns>service in object form.</returns>
        private object GetServices(Type pType)
        {
            return _serviceProvider != null ? _serviceProvider.GetService(pType) : null;
        }
        #endregion
    }
}