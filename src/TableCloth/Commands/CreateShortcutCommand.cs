﻿using System;
using TableCloth.Components;
using TableCloth.Contracts;
using TableCloth.ViewModels;

namespace TableCloth.Commands;

public sealed class CreateShortcutCommand : CommandBase
{
    public CreateShortcutCommand(
        ShortcutCrerator shortcutCrerator)
    {
        _shortcutCreator = shortcutCrerator;
    }

    private readonly ShortcutCrerator _shortcutCreator;

    public override void Execute(object? parameter)
    {
        if (parameter is not ITableClothViewModel viewModel)
            throw new ArgumentException("Selected parameter is not a supported type.", nameof(parameter));

        _shortcutCreator.CreateShortcut(viewModel);
    }
}
