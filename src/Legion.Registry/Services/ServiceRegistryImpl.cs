using System.Collections.Concurrent;
using Grpc.Core;
using Legion.Common.V1;
using Legion.Registry.V1;

namespace Legion.Registry.Services;

public sealed class ServiceRegistryImpl : ServiceRegistry.ServiceRegistryBase
{
    private static readonly ConcurrentDictionary<string, ServiceInstance> _instancesById = new();

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request?.Key?.Name) ||
            string.IsNullOrWhiteSpace(request?.Key?.Version) ||
            string.IsNullOrWhiteSpace(request?.Endpoint?.Address))
        {
            return Task.FromResult(new RegisterResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidArgument,
                        Message = "Key.name, Key.version, and Endpoint.address are required."
                    }
                }
            });
        }

        var id = string.IsNullOrWhiteSpace(request.RequestedId)
            ? Guid.NewGuid().ToString("N")
            : request.RequestedId;

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var instance = new ServiceInstance
        {
            Id = new ServiceId { Value = id },
            Key = request.Key,
            Endpoint = request.Endpoint,
            Metadata = request.Metadata ?? new ServiceMetadata(),
            RegisteredAtMs = nowMs,
            LastSeenAtMs = 0
        };

        _instancesById[id] = instance;

        return Task.FromResult(new RegisterResponse
        {
            Result = new Result { Ok = new Empty() },
            Instance = instance
        });
    }

    public override Task<DeregisterResponse> Deregister(DeregisterRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request?.Id?.Value))
        {
            return Task.FromResult(new DeregisterResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidArgument,
                        Message = "ServiceId.value is required."
                    }
                }
            });
        }

        var removed = _instancesById.TryRemove(request.Id.Value, out _);

        return Task.FromResult(new DeregisterResponse
        {
            Result = new Result { Ok = new Empty() },
            Removed = removed
        });
    }

    public override Task<ListResponse> List(ListRequest request, ServerCallContext context)
    {
        IEnumerable<ServiceInstance> results = _instancesById.Values;

        if (!string.IsNullOrWhiteSpace(request?.Name))
            results = results.Where(x => x.Key?.Name == request.Name);

        if (!string.IsNullOrWhiteSpace(request?.Version))
            results = results.Where(x => x.Key?.Version == request.Version);

        if (request?.Tags?.Count > 0)
            results = results.Where(x => x.Metadata?.Tags?.Any(t => request.Tags.Contains(t)) == true);

        return Task.FromResult(new ListResponse
        {
            Result = new Result { Ok = new Empty() },
            Instances = { results }
        });
    }

    public override Task<ResolveResponse> Resolve(ResolveRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request?.Key?.Name) || string.IsNullOrWhiteSpace(request?.Key?.Version))
        {
            return Task.FromResult(new ResolveResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidArgument,
                        Message = "ServiceKey.name and ServiceKey.version are required."
                    }
                }
            });
        }

        var match = _instancesById.Values.FirstOrDefault(x =>
            x.Key?.Name == request.Key.Name &&
            x.Key?.Version == request.Key.Version);

        if (match is null)
        {
            return Task.FromResult(new ResolveResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.NotFound,
                        Message = $"No instance found for {request.Key.Name}@{request.Key.Version}"
                    }
                }
            });
        }

        return Task.FromResult(new ResolveResponse
        {
            Result = new Result { Ok = new Empty() },
            Instance = match
        });
    }
}