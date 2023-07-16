// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "ExternalInit", Scope = "namespace", Target = "~N:System.Runtime.CompilerServices")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVVM", Scope = "member", Target = "~M:FantasyFootball.ViewModels.GamesViewModel.SimulateAgain~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVVM", Scope = "member", Target = "~M:FantasyFootball.ViewModels.TeamsViewModel.AddNewTeam~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVVM", Scope = "member", Target = "~P:FantasyFootball.ViewModels.SettingsViewModel.AppVersion")]
