using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using UnityEngine;

namespace Owlcat.QA.Validation
{
	public static class AssetValidatorWrapper
    {
        private static ValidationContext s_ValidationContext;
        private static readonly ValidationStack<object> ValidationObjectsStack = new();
        private static readonly Dictionary<string, bool> BlueprintValidationStatuses = new();

        public static bool HasValidationError(this IScriptableObjectWithAssetId bp)
        {
            return BlueprintValidationStatuses.TryGetValue(bp.AssetGuid, out bool result) && result;
        }

        public static void LogErrorWithPath(this ValidationContext context, ErrorLevel level, string designer, string errorFormat)
        {
            context.AddError(level, designer, $"{errorFormat}. FullPath {context.FullPath}");
        }

        public static void Validate<T>(T validated, ValidationContext vc, int parentIndex) where T :class
        {
            if (validated == null)
            {
                vc.AddError(ErrorLevel.Critical, $"trying validate null object");
                return;
            }

            if (s_ValidationContext != vc)
            {
                ValidationObjectsStack.Clear();
                s_ValidationContext = vc;
            }

            int errorsCount = vc.RawErrors.Count;
            if (validated is IScriptableObjectWithAssetId id)
            {
                BlueprintValidationStatuses[id.AssetGuid] = false;
            }
            try
            {
                switch (validated)
                {
                    case ScriptableObject or IScriptableObjectWithAssetId:
                        AssetValidator.ValidateAsset(validated, vc);
                        break;
                    default:
                        AssetValidator.ValidateExternalTypes(validated, vc, parentIndex);
                        break;
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                vc.AddError($"Exception in validate: {e.Message}");
                PFLog.Default.Exception(e);
            }
            if (validated is IScriptableObjectWithAssetId assetId)
            {
	            BlueprintValidationStatuses[assetId.AssetGuid] = vc.HasErrors && vc.RawErrors.Count > errorsCount;
            }
        }

        private class ValidationStack<T> : IDisposable where T: class
        {
            private readonly LinkedList<T> m_ValidationStack = new();

            private readonly Dictionary<T, int> m_ElementsCounter;

            public ValidationStack()
            {
                m_ElementsCounter = new Dictionary<T, int>(new IdentityEqualityComparer());
            }

            public bool HasCircularDependencies
                => m_ValidationStack.Count > m_ElementsCounter.Count;

            public string FormatValidationStack()
            {
                return m_ValidationStack.Count > 1
                    ? "Validation chain: " + string.Join("->", m_ValidationStack.Select(v => v))
                    : "";
            }

            public void Clear()
            {
                m_ValidationStack.Clear();
            }

            public void Push([NotNull] T obj)
            {
                m_ValidationStack.AddLast(obj);

                m_ElementsCounter.TryGetValue(obj, out int counter);
                m_ElementsCounter[obj] = counter + 1;
            }

            public void Dispose()
            {
                Pop();
            }

            private void Pop()
            {
                var obj = m_ValidationStack.Last.Value;
                m_ValidationStack.RemoveLast();

                m_ElementsCounter.TryGetValue(obj, out int counter);
                if (counter == 1)
                {
                    m_ElementsCounter.Remove(obj);
                }
                else
                {
                    m_ElementsCounter[obj] = counter - 1;
                }
            }
            public IDisposable PrepareNestedValidation(T obj)
            {
                Push(obj);
                return this;
            }
            private class IdentityEqualityComparer : IEqualityComparer<T>
            {
                public bool Equals(T left, T right)
                {
                    return ReferenceEquals(left, right);
                }

                public int GetHashCode(T value)
                {
                    return RuntimeHelpers.GetHashCode(value);
                }
            }
        }
    }

	
}