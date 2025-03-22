 public interface IKsqlTransaction
    {
        /// <summary>
        /// トランザクションをコミット
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// トランザクションを中止
        /// </summary>
        Task AbortAsync();
    }
