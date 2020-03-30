# Azure Durable Functions vs Mass Transit Sagas

<!-- ## Overviews

### Durable Functions

Azure Durable Funcitons provides a framework to orchestrate separate functions.

### Mass Transit Sagas

Sagas are a framework the provide state tracking / orchestration logic for a business pipeline that utilize message queues as pipeline checkpoints. -->

## Theia Necessary Features

1. Supporting long running jobs
    * Durable Functions
        * Activity Triggered functions are still limited to 5 minutes
        * Orchestration Triggered functions are not limited and can initiate multiple functions or call external APIs exceeding 5 minutes overall
        * Orchestration Triggered functions can also be configured to wait on the firing of external events
        * If any single job can't be separated into units of work <5 minutes each, we could use WebJobs or a Dockerized service and initiate those over HTTP
    * MassTransit Sagas
        * Sagas can be created in a dockerized services, so they wouldn't have any limitation on job run time
2. Tracking job state
    * Durable Functions
        * Logging is configurable (we can easily hook into Seq) and would allow easily creating an audit trail
        * The initial orchestration trigger can return HTTP endpoints that can be used to monitor the job. These can be either built in APIs, or custom created
    * MassTransit Sagas
        * Saga state is stored in a repository, which can be in SQL and would thus be queryable directly or accessible via API
3. Scalability
    * Durable Functions
        * Unlimited instances of orchestrator functions can be created, so the pipeline is easily scalable
        * Within one orchestration, multiple instances of one single activity can be created (fan out, fan in pattern)
    * MassTransit Sagas
        * Saga containers can easily be scaled as long as they are using a shared repository to manage concurrency
4. Infrastructure (Deployment)
    * Durable Functions
        * Preferred deployment method is to use Azure Functions continuous deployment functionality, but custom options are available.
    * MassTransit Sagas
        * Deployment would have to be custom configured to build new docker images
5. Error Handling
    * Durable Functions
        * Exceptions in activity functions are passed back to parent orchestrator function and can be handled in either place
        * Retry options are configurable via a built in options class. If configured, retries happen automatically when an activity or sub-orchestration throw an exception
        * If the orchestrator function fails, it's logged in azure storage and marked as failed
        * Manual intervention would need to be built in to restart pipelines at certain points
    * MassTransit Sagas
        * Retries can happen in memory for transient errors (DB timeout, web service busy, etc.) or messages can be redelivered in the future when longer lasting errors (SQL server crashed, web service down, etc.) have been resolved
        * Failed messages are automatically moved to an error queue
        * Manual intervention would be moving messages to certain queues to restart processes
6. Process Visibility
    * For either solution, visibility into the process pipeline would require development of audit tracking in the database. Durable Functions has the advantage of automatically logging the status changes of each running job.

## Comparison

| Feature | Durable Functions | MassTransit Sagas |
| --- | --- | --- |
| **Long Running Jobs** | Possible, but needs to run in a separate service | No separate service needed since the application runs in containers |
| **State tracking** | Built in event/instance logging in Azure Storage | Relies on persistent repository storage, such as SQL database |
| **Scalability** | Scaling is built in, just need to create new instances of orchestrator | Scaling is configurable by scaling number of containers |
| **Deployments** | Capable of CI/CD using Azure tools | Would take some more work to set up CI/DI, but it can be done with containers |
| **Error Handling** | Built in to instance logging. Would need to set up manual restart points | Errors will move messages to an error queue. Process could restart by pushing to an input queue |
| **Other Considerations** | | <ul><li>Queue messages have a defailt time to live of TimeSpan.Max, and if they do expire they would move to the dead letter queue</li></ul> |

## Recommendation

Both solutions are more than capable of meeting the core business needs of the moonshot project, but in my opinion, Durable Functions would be a better choice for these reasons:

* While MassTransit is a production ready open source tool with active development, Durable Functions is also actively developed, and importantly supported by Microsoft. Because of this, managing a Durable Function solution in Azure will be easier and documentation and community support are more robust.
* A solution with MassTransit would require a large amount of Service Bus queues and would add an overhead to resource management that would not exist with Durable Functions.
* Durable Functions have a greater potential for extension, with a variety of triggers available (queue, HTTP)
* While I don't have exact estimates on cost, serverless functions are generally the cheapest option for cloud computation, and therefore Durable Functions should be cheaper to operate.
