# Deep Dive ON AWS Fargate

Course by [Dmitriy Novikov - Amazon Web Services (AWS)](https://www.linkedin.com/in/dmnovikov)

## Register Task Definition

- Define application containers

  1. Image URL
  2. CPU Requirements
  3. Memory Requirements

- Create Cluster

  1. Infrastructure Isolation Boundary
  2. IAM Permissions Boundary

- Inside Cluster
  1. Run Tasks (Task Definition Snipped)
     - A running instantation of a task definition
     - Use Fargate launch type
  2. Create Service
     - Maintain n running copies
     - Integrated with ELB
     - Unhealthy tasks automatically replaced
- Task Definition

  1. Immutable, versioned document
  2. Identified by family: version
  3. Contains a list of up to 10 container definitions
  4. All containers are collocated on the same host
  5. Each container definition has
     - a name
     - Image Url (Amazon ECR or public images)
  6. CPU and Memory Specification

     - Task Level Resources
       1. Total CPU/memory across all containers
       2. Required fields
       3. Billing dimensions
     - Units
       1. CPU: cpu-units. 1 vCPU = 1024 cpu-units
       2. Memory: MB
     - Container level Resources
       1. Defines sharing of task resources among containers
       2. Optional fields

- Pricing

  1. Pay for what you provision
  2. Billed for task level CPU and memory
  3. Per-second billing. One minute minimum

## Networking

Basically its VPC Integration

1. Launch your Fargate Tasks into subnets
2. Under the hood (`VPC Integration` Image)

- AWS create an Elastic Network Interface (ENI)
- The ENI is allocated a private IP from your subnet
- The ENI is attached to your task
- Your task now has a private IP from your subnet

3. User can assign public IPs to their tasks
4. Configure security groups to control inbound and outbound traffic

```
  aws ecs run-task ...
    -- task-definition my-application:1
    -- network-configuration
      "awsvpcConfiguration = {
        subnets=[subnet1-id, subnet2-id],
        securityGroups=[sg-id]
      }"
```

## Storage

Two types of EBS backed Ephemeral storage is there

1. Writable Layer Storage

- Docker images are composed of layers. The topmost layer is the "writable" layer to capture file changes made by running container
- 10GB layer storage available per task across all containers including image layers
- Writes are not visible across containers

2. Volume Storage

- In case need writes to be visible across containers
- Fargate provide 4GB volume space per task
- Configure via volume mounts in task definition
  - Can mount at different container Paths
  - Do not specify host sourcePath
- As this is ephemera too, not available after the task stops

## IAM Permissions

Considering security, there are 3 types of permissions

1. Cluster Permission

- controls who can launch/describe tasks in your cluster

2. Application Permission (Same as EC2 IAM rules)

- Allows your application containers to access AWS resources securely

3. Housekeeping Permissions

- Allows AWS to perform housekeeping activities around your task
  - Amazon ECR Image Pull
  - Cloudwatch Logs pushing
  - ENI Creation
  - Register/Deregister targets into ELB
- AWS need certain permissions in user account to bootstrap their tasks and keep it running
- **Execution role** gives AWS permissions for
  - ECR Image Pull
  - Pushing Cloudwatch logs
- **ECS Service Linked Role** default role gives AWS permissions for:
  - ENI Management
  - ELB Targer Registration/Deregistration

## Visibility and Monitoring

Use of `CloudWatch` Logs Configuration

- Use of `awslogs` driver to send stdout from your application to Cloudwatch logs
- Create a log group in CloudWatch
- Configure the log driver in your task definition

### Other Visibility Tools

- Cloudwatch events on task state changes (Gives feasiblity of the task)
- Service CPU/memory utilization metrics available in Cloudwatch (allows to scale out and scale in based on threshold and criteria of autoscalling)

### Task Metadata

Query environmental data and statistics for running tasks from within the Task! Enables monitoring tools like Datadog etc.
There are 2 endpoints of task metadata

1. Task Level (For all containers) (No. of tasks, date creation, version of container image)

- 169.254.170.2/v2/metadata - Metadata JSON for Task
- 169.254.170.2/v2/stats - Docker stats JSON for all containers in the Task

2. Container Level (Images and docker labels)

- 169.254.170.2/v2/metadata/<container-id>
- 169.254.170.2/v2/stats/<container-id>

## Managed Service Discovery for ECS

1. Service Registry

- Predictable names for services
- Auto updated with latest, healthy IP, port

2. High availability, high scale
3. Extensible: Flexible boundaries for auto discovery

### How it works

1. Use of Amazon Route 53 auto naming provides service registry. Amazon Route 53 provides APIs to create

- Namespace
- CNAME per service autoname
- A record per task IP
- SRV records per task IP + port

2. ECS Schedules and Places Service Endpoints

- ECS Scheduler updates on:
  - Service Scaling
    1. Task Registration
    2. Rask De-registrations
  - Task Health
  - Scheduling / Placement changes
  - ECS instance changes
- ECS maintains latest state of the dynamic environment in Service Registry
- Benefits
  - Managed Ease of Operations
  - Highly available (Tied to Amazon Route 53)
  - Extensible (Words across AWS services)

## Demo

It creates of 2 parts

1. Task Definition and starting a task out of it
2. Building CI/CD pipeline for Fargate

Already created resources (ELB, security groups which needs to be bind to the demo)

### Working

1. Create a cluster

```
  aws ecs create-cluster --cluster-name pictshare --region ap-southeast-2
```

2. Register the task definition on AWS

```
  aws ecs register-task-definition \
  --cli-input-json file://task_definition.json \
  --region ap-southeast-2 --query
```

3. Creating a service

```
  aws ecs create-service --cli-input-json file://service.json
```

4. Add scalability for the application

```
  aws application-autoscalling register-scalable-target --resource-id service/pictshare/pictshare --service-namespace ecs --scalable-dimension ecs:service:DesiredCount --min-capacity 1 --max-capacity 20 --role-arn arn:aws:iam::   :role/ecsServiceAutoScalingRole
```

5. Scale out (A JSON file which tells fargate to scale application on the criteria when CPU usage goes above 50%)

```
  aws application-autoscaling put-scaling-policy --cli-input-json file://sclae-out.json
```

6. Getting URL of the loadbalancer to check the site running or not

```
  url=$(aws elbv2 describe-load-balancers \
  --load-balancer-arns arn:aws:elasticloadbalancing:ap-southeast-2: :loadbalancer/app/pictshare-fargate-demo/cb5435435323432 \
  | jq '.LoadBalancers[].DNSName' | sed -e 's/^"//' -e 's/"$//') && echo $url
```

```
  curl -I (URL)
```

7. Create a repository

```
  aws codecommit create-repository --repository-name pictshare
```

Need to connect the repo to local github repo

8. Codebuild command to build a new image of docker app
   It's a container which create a new container of the app

```
  aws codebuild create-project \
  --name "pictshare" \
  --description "Build project for Pictshare" \
  --source type="CODEPIPELINE" \
  --service-role "arn:aws:iam:: :role/CodeBuildExecutionRole" \
  --environment type="LINUX_CONTAINER", \
  image="aws/codebuild/docker:17.09.0", \
  computeType="BUILD_GENERAL1_SMALL", \
  environmentVariables="[{name=AWS_DEFAULT_REGION,value='ap-southeast-2'}, {name=AWS_ACCOUNT_ID,value=''},{name=REPOSITORY_URI,value=arn:aws:codecommit:ap-southeast-2: :pictshare}]" \
  --artifacts type="CODEPIPELINE"
```

9. Create a Pipeline

```
  aws codepipeline create-pipeline \
  --pipeline file://pipeline_structure.json
```

10. Event Rule to trigger CI/CD on code commit to `master` branch

```
  aws events put-rule \
  --cli-input-json file://event_put_rule.json
```

ARN Rule attained from above command needs to put in target

```
  aws events put-targets \
  --rule CodeCommitRulePictshare \
  --targets Id=1,Arn=arn:aws:codepipeline:ap-southeast-2: :pictshare,RoleArn={Received as in above command}
```
