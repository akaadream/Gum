using Gum.Wireframe;
using MonoGameInWpf.MonoGameControls;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Gui
{
    public class RenderingLibraryWpfControl : MonoGameContentControl
    {
        ServiceContainer services = new ServiceContainer();

        /// <summary>
        /// Gets an IServiceProvider containing our IGraphicsDeviceService.
        /// This can be used with components such as the ContentManager,
        /// which use this service to look up the GraphicsDevice.
        /// </summary>
        public ServiceContainer Services
        {
            get { return services; }
        }

        public SystemManagers SystemManagers { get; private set; }

        public RenderingLibraryWpfControl()
        {
            SystemManagers = new SystemManagers();
        }

        protected override void OnXnaInitialize()
        {
            SystemManagers.Initialize(GraphicsDevice);

            base.OnXnaInitialize();
        }
    }
}
