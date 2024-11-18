using System;

namespace RedisCore.Internal.Protocol;

public class ProtocolException(string message) : Exception(message);