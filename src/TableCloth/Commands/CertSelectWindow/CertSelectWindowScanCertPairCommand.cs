﻿using System;
using System.Linq;
using TableCloth.Components;
using TableCloth.ViewModels;

namespace TableCloth.Commands.CertSelectWindow;

public sealed class CertSelectWindowScanCertPairCommand : CommandBase
{
    public CertSelectWindowScanCertPairCommand(
        X509CertPairScanner certPairScanner)
    {
        _certPairScanner = certPairScanner;
    }

    private readonly X509CertPairScanner _certPairScanner;

    public override void Execute(object? parameter)
    {
        if (parameter is not CertSelectWindowViewModel viewModel)
            throw new ArgumentException("Selected parameter is not a supported type.", nameof(parameter));

        viewModel.SelectedCertPair = default;
        viewModel.CertPairs = _certPairScanner.ScanX509Pairs(
            _certPairScanner.GetCandidateDirectories()).ToList();

        if (viewModel.CertPairs.Count == 1)
            viewModel.SelectedCertPair = viewModel.CertPairs.Single();
    }
}
