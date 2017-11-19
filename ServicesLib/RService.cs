using REngine;

namespace ServicesLib
{
    public class IrService : IRExecuter
    {
        private IRExecuter _irExecuter;

        public InteractiveR InteractiveR
        {
            set
            {
                _irExecuter = new InteractiveRExecuter(value);
            }
        }

        public IrService()
        {
            if (ServiceContainer.EnvironmentService().IsLocal)
            {
                //_irExecuter = new LocalRExecuter();
                InteractiveR = new InteractiveR();
            }
            else
            {
                _irExecuter = new RemoteRExecuter();
            }
        }

        public string RunRScript(string script, byte[] inputData)
        {
            return _irExecuter.RunRScript(script, inputData);
        }
    }
}
