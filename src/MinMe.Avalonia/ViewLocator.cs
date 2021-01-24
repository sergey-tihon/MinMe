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
            var fullName = data.GetType().FullName;
            if (fullName is null)
                throw new Exception("Data Type.FullName is null");

            var name = fullName.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type is null)
                return new TextBlock {Text = "Not Found: " + name};

            var instance = Activator.CreateInstance(type);
            return instance switch
            {
                Control control => control,
                _ => throw new Exception($"Unexpected instance type {instance?.GetType().Name}")
            };
        }

        public bool Match(object data) =>
            data is ViewModelBase;
    }
}
