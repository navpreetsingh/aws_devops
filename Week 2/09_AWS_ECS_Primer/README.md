# ECS Primer

In this course, how ECS services are deployed via EC2 or Fargate is in focus

## Placement Strategies

1. Random - places services randomly across container instances
2. Binpack - places tasks on least amount of CPU or memory. Minimized no. of instances and get most utilization of every instance
3. Spread - place task evenly across instances in specific availability timezones

## Placement Constraints (Binding)

Prevent task placements

1. distinctInstance - place each task on different container instance
2. memberOf - which place task based on expression (Group membership)

## Amazon ECR

- Fully managed cloud based Docker image registry
- scalable and highly available
- Secure
  1. At rest encryption
  2. IAM based access and authorization controls
- Fully integrated with ECS and Docker CLI

## Daemon Scheduing Strategy

Scheduling each task/service on each instance in the cluster
