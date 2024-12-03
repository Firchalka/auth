// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;

namespace IdentityServerHost.Quickstart.UI
{
    public class GrantsViewModel
    {
        public IEnumerable<GrantViewModel> Grants { get; set; }
    }

    public class GrantViewModel
    {
        public string ClientId { get; init; }
        public string ClientName { get; init; }
        public string ClientUrl { get; set; }
        public string ClientLogoUrl { get; init; }
        public string Description { get; init; }
        public DateTime Created { get; init; }
        public DateTime? Expires { get; init; }
        public IEnumerable<string> IdentityGrantNames { get; init; }
        public IEnumerable<string> ApiGrantNames { get; init; }
    }
}