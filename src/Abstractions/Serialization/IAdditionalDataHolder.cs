// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization
{

    /// <summary>
    /// Defines a contract for models that can hold additional data besides the described properties.
    /// </summary>
    public interface IAdditionalDataHolder
    {

        /// <summary>
        ///  Stores the additional data for this object that did not belong to the properties.
        /// </summary>
        IDictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Avoid creating a new empty instance AdditionalData
    /// </summary>
    public interface IAdditionalDataHolderSlim : IAdditionalDataHolder {

        /// <summary>
        /// Get <see cref="IAdditionalDataHolder.AdditionalData"/> may be null.
        /// </summary>
        /// <returns></returns>
        IDictionary<string, object>? GetAdditionalData();
    }
}
