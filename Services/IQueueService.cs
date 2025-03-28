namespace HW4NoteKeeper.Services
{
    /// <summary>
    /// Interface for enqueuing messages to an Azure Storage Queue.
    /// </summary>
    public interface IQueueService
    {
        /// <summary>
        /// Enqueues a message to the specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="message">The message object to enqueue.</param>
        /// <returns>A task that represents the asynchronous operation, containing a bool indicating success.</returns>
        Task<bool> EnqueueMessageAsync(string queueName, object message);
    }
}
