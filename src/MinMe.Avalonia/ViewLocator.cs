// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MinMe.Avalonia.ViewModels;

namespace MinMe.Avalonia
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            var name = data.GetType().FullName.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type is null)
                return new TextBlock {Text = "Not Found: " + name};

            return (Control)Activator.CreateInstance(type);
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}