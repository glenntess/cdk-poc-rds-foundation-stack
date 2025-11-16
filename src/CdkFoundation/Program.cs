using Amazon.CDK;

namespace CdkFoundation
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CdkFoundationStack(app, "CdkFoundationStack");
            app.Synth();
        }
    }
}
