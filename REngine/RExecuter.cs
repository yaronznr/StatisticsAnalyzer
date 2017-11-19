
namespace REngine
{
    public interface IRExecuter
    {
        string RunRScript(string script, byte[] inputData);
    }

    public class LocalRExecuter : IRExecuter
    {
        public string RunRScript(string script, byte[] inputData)
        {
            return RHelper.RunScript(script, inputData);
        }
    }

    public class InteractiveRExecuter : IRExecuter
    {
        private InteractiveR _rEngine;
        public InteractiveRExecuter(InteractiveR rEngine)
        {
            _rEngine = rEngine;
        }

        public string RunRScript(string script, byte[] inputData)
        {
            return RHelper.RunScript(script, inputData, _rEngine);
        }
    }

    public class RemoteRExecuter : IRExecuter
    {
        public string RunRScript(string script, byte[] inputData)
        {
            return RHelper.RunRemoteScript(script, inputData);
        }        
    }
}
