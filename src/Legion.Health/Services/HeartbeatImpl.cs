using System.Collections.Concurrent;
using Grpc.Core;
using Legion.Common.V1;
using Legion.Health.V1;

namespace Legion.Health.Services;

public sealed class HeartbeatImpl : Heartbeat.HeartbeatBase
{
    private sealed record HeartbeatState(long LastSeenMs, ServiceStatus Status, string Note);

    private static readonly ConcurrentDictionary<string, HeartbeatState> _stateByServiceId = new();

    public override Task<BeatResponse> Beat(BeatRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request?.Id?.Value))
        {
            return Task.FromResult(new BeatResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidArgument,
                        Message = "ServiceId.value is required."
                    }
                },
                Accepted = false
            });
        }

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var status = request.Status == ServiceStatus.Unspecified
            ? ServiceStatus.Healthy
            : request.Status;

        _stateByServiceId[request.Id.Value] = new HeartbeatState(nowMs, status, request.Note ?? "");

        return Task.FromResult(new BeatResponse
        {
            Result = new Result { Ok = new Empty() },
            Accepted = true,
            NextIntervalMs = 3000
        });
    }

    public override Task<CheckResponse> Check(CheckRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request?.Id?.Value))
        {
            return Task.FromResult(new CheckResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidArgument,
                        Message = "ServiceId.value is required."
                    }
                },
                Status = ServiceStatus.Unknown
            });
        }

        if (!_stateByServiceId.TryGetValue(request.Id.Value, out var state))
        {
            return Task.FromResult(new CheckResponse
            {
                Result = new Result
                {
                    Error = new Error
                    {
                        Code = ErrorCode.NotFound,
                        Message = "No heartbeat record for this ServiceId."
                    }
                },
                Status = ServiceStatus.Unknown
            });
        }

        return Task.FromResult(new CheckResponse
        {
            Result = new Result { Ok = new Empty() },
            Status = state.Status,
            LastSeenAtMs = state.LastSeenMs,
            Note = state.Note
        });
    }
}