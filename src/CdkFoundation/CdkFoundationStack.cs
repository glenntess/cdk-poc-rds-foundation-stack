using Constructs;
using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CloudTrail;
using Amazon.CDK.AWS.EC2;

namespace CdkFoundation
{
    public class CdkFoundationStack : Stack
    {
        internal CdkFoundationStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // ----------------------------
            // CloudTrail + S3 bucket
            // ----------------------------
            var trailBucket = new Bucket(this, "SecOpsCloudTrailBucket", new BucketProps {
                BucketName = "glt-secops-cloudtrail",
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                Versioned = true,
                RemovalPolicy = RemovalPolicy.DESTROY, // PoC only
                AutoDeleteObjects = true
            });

            new Trail(this, "SecOpsCloudTrail", new TrailProps {
                TrailName = "glt-baseline-cloudtrail-secops",
                Bucket = trailBucket,
                IsMultiRegionTrail = true,
                IncludeGlobalServiceEvents = true,
                EnableFileValidation = true
            });

            // ----------------------------
            // VPC + Subnets
            // ----------------------------
            var vpc = new CfnVPC(this, "GltPocDevVpc", new CfnVPCProps {
                CidrBlock = "10.207.130.0/23",
                Tags = new[] { new CfnTag { Key = "Name", Value = "glt-poc-dev" } }
            });

            var vpcCidrAssoc = new CfnVPCCidrBlock(this, "VpcAssoc192", new CfnVPCCidrBlockProps {
                VpcId = vpc.Ref,
                CidrBlock = "240.0.0.0/16"
            });

            // Application subnets (ap-southeast-2a/b/c)
            var appAzA = new CfnSubnet(this, "SubnetApplicationAzA", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "10.207.130.0/25",
                AvailabilityZone = "ap-southeast-2a",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "application-az-a" } }
            });

            var appAzB = new CfnSubnet(this, "SubnetApplicationAzB", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "10.207.130.128/25",
                AvailabilityZone = "ap-southeast-2b",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "application-az-b" } }
            });

            var appAzC = new CfnSubnet(this, "SubnetApplicationAzC", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "10.207.131.0/25",
                AvailabilityZone = "ap-southeast-2c",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "application-az-c" } }
            });

            // Isolated subnets (ap-southeast-2a/b/c)
            var isoAzA = new CfnSubnet(this, "SubnetIsolatedAzA", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "240.0.0.0/25",
                AvailabilityZone = "ap-southeast-2a",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "isolated-az-a" } }
            });
            isoAzA.AddDependency(vpcCidrAssoc);

            var isoAzB = new CfnSubnet(this, "SubnetIsolatedAzB", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "240.0.0.128/25",
                AvailabilityZone = "ap-southeast-2b",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "isolated-az-b" } }
            });
            isoAzB.AddDependency(vpcCidrAssoc);

            var isoAzC = new CfnSubnet(this, "SubnetIsolatedAzC", new CfnSubnetProps {
                VpcId = vpc.Ref,
                CidrBlock = "240.0.1.0/25",
                AvailabilityZone = "ap-southeast-2c",
                MapPublicIpOnLaunch = false,
                Tags = new[] { new CfnTag { Key = "Name", Value = "isolated-az-c" } }
            });
            isoAzC.AddDependency(vpcCidrAssoc);

            // ----------------------------
            // NACLs
            // ----------------------------
            // Application NACL: allow all
            var naclApp = new CfnNetworkAcl(this, "NaclApplication", new CfnNetworkAclProps {
                VpcId = vpc.Ref,
                Tags = new[] { new CfnTag { Key = "Name", Value = "nacl-glt-poc-dev-application" } }
            });

            new CfnNetworkAclEntry(this, "NaclAppInboundAllowAll", new CfnNetworkAclEntryProps {
                NetworkAclId = naclApp.Ref,
                RuleNumber = 100,
                Protocol = -1,
                RuleAction = "allow",
                Egress = false,
                CidrBlock = "0.0.0.0/0"
            });

            new CfnNetworkAclEntry(this, "NaclAppOutboundAllowAll", new CfnNetworkAclEntryProps {
                NetworkAclId = naclApp.Ref,
                RuleNumber = 100,
                Protocol = -1,
                RuleAction = "allow",
                Egress = true,
                CidrBlock = "0.0.0.0/0"
            });

            new CfnSubnetNetworkAclAssociation(this, "AssocNaclAppAzA", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclApp.Ref,
                SubnetId = appAzA.Ref
            });
            new CfnSubnetNetworkAclAssociation(this, "AssocNaclAppAzB", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclApp.Ref,
                SubnetId = appAzB.Ref
            });
            new CfnSubnetNetworkAclAssociation(this, "AssocNaclAppAzC", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclApp.Ref,
                SubnetId = appAzC.Ref
            });

            // Isolated NACL: allow only from application subnets
            var naclIso = new CfnNetworkAcl(this, "NaclIsolated", new CfnNetworkAclProps {
                VpcId = vpc.Ref,
                Tags = new[] { new CfnTag { Key = "Name", Value = "nacl-glt-poc-dev-isolated" } }
            });

            string[] appCidrs = { "10.207.130.0/25", "10.207.130.128/25", "10.207.131.0/25" };
            int rule = 100;
            foreach (var cidr in appCidrs)
            {
                new CfnNetworkAclEntry(this, $"NaclIsoInbound_{cidr.Replace('/', '_')}", new CfnNetworkAclEntryProps {
                    NetworkAclId = naclIso.Ref,
                    RuleNumber = rule,
                    Protocol = -1,
                    RuleAction = "allow",
                    Egress = false,
                    CidrBlock = cidr
                });
                new CfnNetworkAclEntry(this, $"NaclIsoOutbound_{cidr.Replace('/', '_')}", new CfnNetworkAclEntryProps {
                    NetworkAclId = naclIso.Ref,
                    RuleNumber = rule,
                    Protocol = -1,
                    RuleAction = "allow",
                    Egress = true,
                    CidrBlock = cidr
                });
                rule += 10;
            }

            new CfnSubnetNetworkAclAssociation(this, "AssocNaclIsoAzA", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclIso.Ref,
                SubnetId = isoAzA.Ref
            });
            new CfnSubnetNetworkAclAssociation(this, "AssocNaclIsoAzB", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclIso.Ref,
                SubnetId = isoAzB.Ref
            });
            new CfnSubnetNetworkAclAssociation(this, "AssocNaclIsoAzC", new CfnSubnetNetworkAclAssociationProps {
                NetworkAclId = naclIso.Ref,
                SubnetId = isoAzC.Ref
            });

            // Single route table for all subnets
            var defaultRtb = new CfnRouteTable(this, "DefaultRouteTable", new CfnRouteTableProps
            {
                VpcId = vpc.Ref,
                Tags = new[] { new CfnTag { Key = "Name", Value = "default-rtb" } }
            });

            // Associate route table with all subnets
            void AssocRt(string id, string subnetId)
            {
                new CfnSubnetRouteTableAssociation(this, id, new CfnSubnetRouteTableAssociationProps
                {
                    RouteTableId = defaultRtb.Ref,
                    SubnetId = subnetId
                });
            }

            AssocRt("AssocRtAppAzA", appAzA.Ref);
            AssocRt("AssocRtAppAzB", appAzB.Ref);
            AssocRt("AssocRtAppAzC", appAzC.Ref);

            AssocRt("AssocRtIsoAzA", isoAzA.Ref);
            AssocRt("AssocRtIsoAzB", isoAzB.Ref);
            AssocRt("AssocRtIsoAzC", isoAzC.Ref);

            // Done: VPC, subnets, NACLs, CloudTrail + S3 bucket
        }
    }
}
