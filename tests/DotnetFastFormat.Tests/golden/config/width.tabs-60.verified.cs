namespace Company.Product;

public class OrderProcessingService :
	ServiceBase,
	IOrderProcessor,
	IDisposable,
	IAsyncDisposable
{
	public OrderProcessingService(
		string connectionName,
		int retryCount,
		TimeSpan timeout
	)
	{ }

	public void Process(
		string customerName,
		string orderNumber,
		int quantity,
		bool express
	)
	{ }

	public void ProcessBatch(
		string customerName,
		string orderNumber,
		int quantity,
		bool express,
		int priority
	)
	{ }

	public void Reconcile(
		string customerName,
		string orderNumber,
		string correlationId,
		int quantity,
		bool express,
		int priority
	)
	{ }

	public int Total { get; set; } = 0;

	public void Short(int a, int b) { }
}

public record Customer(
	string FirstName,
	string LastName,
	string EmailAddress,
	int Age,
	bool Active
);

public interface IStore<TKey, TValue>
	where TKey : notnull
	where TValue : class, new()
{
	TValue Find(
		TKey key,
		CancellationToken cancellationToken,
		bool includeDeleted,
		string tenant
	);
}
