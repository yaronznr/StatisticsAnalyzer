using System;
using Newtonsoft.Json.Serialization;

namespace WebApp
{
    public class DictionaryContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
        {
            var contract = base.CreateDictionaryContract(objectType);
            contract.PropertyNameResolver = str => str; // Keep casing for dictionary strings
            return contract;
        }
    }
}