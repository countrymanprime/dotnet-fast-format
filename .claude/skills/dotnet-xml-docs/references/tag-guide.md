# XML doc tag guide

These defaults follow the conventions of the .NET API docs (`dotnet/dotnet-api-docs` wiki) and
the Microsoft Learn page "Recommended XML tags for C# documentation comments". Where the repo
already has a different convention, follow the repo.

## Opening phrases for each kind of member

| Member | Summary pattern |
| --- | --- |
| Class / struct / record | `Represents …` / `Provides …` (e.g. "Represents a single line item in an order.") |
| Static class | `Provides … methods for …` / `Provides extension methods for …` |
| Interface | `Defines …` (e.g. "Defines a mechanism for …") |
| Constructor | `Initializes a new instance of the <see cref="Foo"/> class.` For structs, say "structure". With arguments: `… class with the specified <paramref name="name"/>.` |
| Property (get/set) | `Gets or sets …` |
| Property (get only) | `Gets …` |
| Boolean property | `Gets a value indicating whether …` or `Gets or sets a value indicating whether …` (StyleCop SA1623 checks this) |
| Method | Third-person verb: `Returns …`, `Creates …`, `Parses …`, `Removes …` |
| `Try*` method | `Attempts to …` (e.g. `Attempts to parse the specified text as a <see cref="Money"/> value.`) |
| Async method | `Asynchronously …` or the plain verb, whichever the repo uses. Be consistent. |
| Event | `Occurs when …` |
| Enum type | `Specifies …` (e.g. "Specifies how retries are scheduled.") |
| Enum member | What the value means: "Retries are spaced by a fixed delay." |
| Exception class | `The exception that is thrown when …` |
| Delegate | `Represents the method that …` |
| Constant / static readonly field | `Represents …` or state the value's meaning |
| `Equals`, `GetHashCode`, `ToString`, `Dispose`, interface impls, overrides | `/// <inheritdoc/>` |

## Standard phrasings for common parameters and return values

- **Boolean return**
  `<returns><see langword="true"/> if …; otherwise, <see langword="false"/>.</returns>`
- **`Task` return**
  `<returns>A task that represents the asynchronous operation.</returns>`
- **`Task<T>` return**
  `<returns>A task that represents the asynchronous operation. The task result contains …</returns>`
- **`ValueTask` return**
  Same as `Task`, with "value task" in place of "task".
- **`CancellationToken`**
  `<param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>`
  Drop the second sentence if the parameter has no default.
- **`out` value of a `Try*` method**
  `<param name="result">When this method returns, contains … if the operation succeeded; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>`
- **Collection that is never null**
  "… An empty collection if there are none." It's a contract, so say it.

## Exceptions

Document each exception thrown **directly** by the member, including by guard clauses. Don't
list exceptions from arbitrary callees unless they are part of the contract.

**Default style: state the condition, as if preceded by "if".**

```csharp
/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
/// <exception cref="ArgumentException">
///   <para><paramref name="path"/> is empty.</para>
///   <para>-or-</para>
///   <para><paramref name="path"/> contains invalid characters.</para>
/// </exception>
/// <exception cref="InvalidOperationException">The reader has not been started.</exception>
/// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
```

- **Repo uses "Thrown when …" instead?** Use it everywhere, not both.
- **Async methods.** Argument validation that runs before the first `await` throws synchronously.
  An exception thrown after that is stored in the task. When the difference matters, add "This
  exception is stored into the returned task." to that exception's entry.
- **Cancellation.** Document `OperationCanceledException` as "The cancellation token was canceled.
  This exception is stored into the returned task."

## Tags

| Tag | Rules |
| --- | --- |
| `<summary>` | One sentence, ending with a period. Say what the member is for. |
| `<param name="x">` | One for every parameter, in signature order. Cover meaning, units and range, and what `null` means. Never just "The x." |
| `<typeparam name="T">` | One for every type parameter. Include any constraints that matter to callers. |
| `<returns>` | What the value **means**, including the not-found or empty case. Don't restate the type. |
| `<value>` | Use on a property when its meaning, unit, or default needs more than the summary. |
| `<exception cref="…">` | See above. |
| `<remarks>` | Usage guidance, thread safety, ownership, invariants, and "Notes to inheritors" for virtual members. Use `<para>` to separate paragraphs. Don't narrate the implementation. |
| `<example>` + `<code language="csharp">` | Entry-point APIs only. Must compile. Escape `<` as `&lt;`, or wrap the code in `<![CDATA[ … ]]>`. |
| `<inheritdoc/>` | Overrides and interface implementations. `<inheritdoc cref="Other.Member"/>` borrows docs from a specific member, e.g. an async method borrowing from its sync twin. |
| `<see cref="…"/>` | Inline link to a code element. The compiler checks it (CS1574). |
| `<see langword="…"/>` | `null`, `true`, `false`, `default`, `async`, `await`, and so on. |
| `<see href="https://…">text</see>` | External link. Note that `cref` never works for URLs. |
| `<seealso cref="…"/>` | "See also" links. Top level only; don't nest it inside `<summary>`. |
| `<paramref>` / `<typeparamref>` | Refer to a parameter or type parameter in prose. |
| `<c>` | Inline code that isn't a resolvable symbol, e.g. `<c>"yyyy-MM-dd"</c>`. |
| `<list type="bullet\|number\|table">` | `<item><description>…</description></item>`. Tables use `<listheader>` and `<term>`. |
| `<include file="…" path="…"/>` | Pulls docs in from an external XML file. Rarely worth it, because it hides docs from readers of the source. |

## `cref` syntax

```csharp
<see cref="List{T}"/>                                  // generic type: braces, not angle brackets
<see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
<see cref="Parse(string)"/>                            // pick one overload explicitly
<see cref="Parse(string, IFormatProvider?)"/>
<see cref="Money(decimal, string)"/>                   // constructor
<see cref="operator +(Money, Money)"/>                 // operator
<see cref="Money.Zero"/>
```

A bare method name with multiple overloads is ambiguous and gives CS0419. Name the overload
you mean.

## Gold-standard example

```csharp
/// <summary>
/// Represents a thread-safe, size-bounded cache whose entries expire after a fixed time to live.
/// </summary>
/// <typeparam name="TKey">The type of the keys. Keys are compared using the comparer supplied at construction.</typeparam>
/// <typeparam name="TValue">The type of the cached values.</typeparam>
/// <remarks>
/// <para>All members are safe to call concurrently.</para>
/// <para>The cache never disposes values. Callers that store <see cref="IDisposable"/> values own their lifetime.</para>
/// </remarks>
public sealed class ExpiringCache<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Attempts to remove the entry with the specified key and return its value.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">
    /// When this method returns, contains the removed value if a live entry was found;
    /// otherwise, the default value of <typeparamref name="TValue"/>. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a live (non-expired) entry was found and removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The cache has been disposed.</exception>
    /// <remarks>Expired entries are treated as absent; this method never returns an expired value.</remarks>
    /// <seealso cref="TryGetValue(TKey, out TValue)"/>
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value) { /* … */ }

    /// <summary>
    /// Gets the value for the specified key, creating and caching it if no live entry exists.
    /// </summary>
    /// <param name="key">The key of the entry to get or create.</param>
    /// <param name="factory">
    /// The function that creates the value. It is called at most once per miss. Concurrent callers
    /// for the same key wait for the first call rather than invoking <paramref name="factory"/> again.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached or newly created value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was canceled. This exception is stored into the returned task.
    /// </exception>
    public Task<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> factory, CancellationToken cancellationToken = default) { /* … */ }
}
```

Why this is good:

- Every tag says something the signature can't: expiry semantics, ownership, concurrency, how often `factory` is called, and where exceptions surface.
- There is nothing about how it's implemented. There is no mention of locks or dictionaries.

## Before → after

```csharp
// Before: restates names, says nothing about the contract
/// <summary>Gets the user.</summary>
/// <param name="id">The id.</param>
/// <returns>The user.</returns>
public Task<User?> GetUserAsync(int id);

// After
/// <summary>Retrieves the user with the specified identifier.</summary>
/// <param name="id">The database identifier of the user. Must be positive.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains the user,
/// or <see langword="null"/> if no user has that identifier.
/// </returns>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is less than or equal to zero.</exception>
public Task<User?> GetUserAsync(int id);
```
