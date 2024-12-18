﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VRCOSC.App.Utils;

namespace VRCOSC.App.Serialisation;

/// <summary>
/// Allows for serialising TReference into TSerialisable and deserialising TSerialisable to pass to <see cref="ExecuteAfterDeserialisation"/>
/// </summary>
/// <typeparam name="TReference"></typeparam>
/// <typeparam name="TSerialisable"></typeparam>
public abstract class Serialiser<TReference, TSerialisable> : ISerialiser where TSerialisable : class
{
    private readonly object serialisationLock = new();

    protected readonly TReference Reference;

    protected virtual string Directory => string.Empty;
    protected virtual string FileName => string.Empty;

    private readonly Storage baseStorage;

    protected Serialiser(Storage storage, TReference reference)
    {
        baseStorage = storage;
        Reference = reference;
    }

    public string FullPath => baseStorage.GetStorageForDirectory(Directory).GetFullPath(FileName);
    public bool DoesFileExist() => baseStorage.GetStorageForDirectory(Directory).Exists(FileName);

    public bool TryGetVersion([NotNullWhen(true)] out int? version)
    {
        if (!DoesFileExist())
        {
            version = null;
            return false;
        }

        try
        {
            lock (serialisationLock)
            {
                var data = JsonConvert.DeserializeObject<SerialisableVersion>(Encoding.Unicode.GetString(File.ReadAllBytes(FullPath)));

                if (data is null)
                {
                    version = null;
                    return false;
                }

                version = data.Version;
                return true;
            }
        }
        catch
        {
            version = null;
            return false;
        }
    }

    public DeserialisationResult Deserialise(string filePathOverride = "")
    {
        var filePath = string.IsNullOrEmpty(filePathOverride) ? FullPath : filePathOverride;

        try
        {
            lock (serialisationLock)
            {
                var data = JsonConvert.DeserializeObject<TSerialisable>(Encoding.Unicode.GetString(File.ReadAllBytes(filePath)));
                if (data is null) return DeserialisationResult.CorruptFile;

                if (ExecuteAfterDeserialisation(data)) Serialise();

                return DeserialisationResult.Success;
            }
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e, $"{GetType().Name} experienced an issue");
            return DeserialisationResult.GenericError;
        }
    }

    public SerialisationResult Serialise()
    {
        try
        {
            lock (serialisationLock)
            {
                var data = (TSerialisable)Activator.CreateInstance(typeof(TSerialisable), Reference)!;
                var bytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(data, Formatting.Indented));
                using var stream = baseStorage.GetStorageForDirectory(Directory).CreateFileSafely(FileName);
                stream.Write(bytes);
            }

            return SerialisationResult.Success;
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e, $"{GetType().Name} experienced an issue");
            return SerialisationResult.GenericError;
        }
    }

    /// <summary>
    /// Executed after the deserialisation is complete
    /// </summary>
    /// <param name="data">The data that has been deserialised</param>
    /// <returns>True if the data should be immediately reserialised</returns>
    protected abstract bool ExecuteAfterDeserialisation(TSerialisable data);

    /// <summary>
    /// Attempts to convert the <paramref name="value"/> to the <paramref name="targetType"/>
    /// </summary>
    /// <remarks>This has some special logic to handle different types automatically</remarks>
    protected bool TryConvertToTargetType(object? value, Type targetType, out object? outValue)
    {
        try
        {
            switch (value)
            {
                case null:
                    outValue = null;
                    return true;

                case JToken token:
                    outValue = token.ToObject(targetType);
                    return true;

                case var subValue when targetType.IsAssignableTo(typeof(Enum)):
                    outValue = Enum.ToObject(targetType, subValue);
                    return true;

                case long utcTicks when targetType == typeof(DateTimeOffset):
                    var utcDateTime = new DateTime(utcTicks, DateTimeKind.Utc);
                    var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
                    outValue = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
                    return true;

                default:
                    outValue = Convert.ChangeType(value, targetType);
                    return true;
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error converting value to target type");
            outValue = null;
            return false;
        }
    }
}
