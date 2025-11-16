using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CloudTrail;

namespace CdkFoundation
{
    public class CdkFoundationStack : Stack
    {
        internal CdkFoundationStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // KMS Key
            var kmsKey = new Key(this, "BaselineRdsKmsKey", new KeyProps {
                Alias = "alias/glt-baseline-rds",
                EnableKeyRotation = true,
                Description = "KMS key for RDS encryption and Secrets Manager"
            });

            // S3 Bucket for CloudTrail
            var trailBucket = new Bucket(this, "SecOpsCloudTrailBucket", new BucketProps {
                BucketName = "glt-secops-cloudtrail",
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // CloudTrail
            new Trail(this, "SecOpsCloudTrail", new TrailProps {
                TrailName = "glt-baseline-cloudtrail-secops",
                Bucket = trailBucket,
                IsMultiRegionTrail = true,
                IncludeGlobalServiceEvents = true,
                EnableFileValidation = true
            });
        }
    }
}
