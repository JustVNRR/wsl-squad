using FluentAssertions;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.ViewModels;

public class SimpleDialogViewModelTests
{
    [Fact]
    public void Confirm_requests_close()
    {
        ConfirmViewModel viewModel = new("Delete 'alpha'?", "Danger");
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Lead.Should().Be("DELETE 'ALPHA'?");
        viewModel.Body.Should().BeEmpty();
        viewModel.Danger.Should().Be("Danger");
        closed.Should().BeTrue();
    }

    [Fact]
    public void Error_formats_the_log_entry()
    {
        ErrorViewModel viewModel = new(new LogEntryDto
        {
            When = new DateTime(2026, 10, 18, 7, 4, 2, DateTimeKind.Utc),
            Level = "ERROR",
            Message = "boom",
        });

        viewModel.Lead.Should().Be("boom");
        viewModel.Stamp.Should().Be("2026-10-18 07:04:02 [ERROR]");
    }

    [Fact]
    public void Archive_returns_a_core_dto_and_sees_an_existing_archive()
    {
        ArchiveViewModel viewModel = new("alpha", replacesArchive: true);

        viewModel.TakenVisible.Should().BeTrue();
        viewModel.Lead.Should().Be("BACK UP 'ALPHA' AS 'ALPHA_ARCHIVE'.");

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(new ArchiveDto
        {
            InstanceName = "alpha",
            Format = "tar.gz",
            Permit = true,
        });
    }

    [Fact]
    public void Compact_carries_the_archive_choice()
    {
        CompactViewModel viewModel = new("alpha", replacesArchive: true) { ArchiveFirst = true };

        viewModel.ReplacesArchive.Should().BeTrue();
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(new CompactDto { InstanceName = "alpha", ArchiveFirst = true });
    }

    [Fact]
    public void Duplicate_validates_the_target_name()
    {
        DuplicateViewModel viewModel = new("alpha", ["alpha"]);

        viewModel.Name.Should().Be("alpha-copy");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();

        viewModel.Name = "bad name";
        viewModel.NameError.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();

        viewModel.Name = "alpha";
        viewModel.NameError.Should().Contain("already exists");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();

        viewModel.Name = "alpha-two";
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(new DuplicateDto { SourceName = "alpha", CopyName = "alpha-two" });
    }

    [Fact]
    public void Restore_rejects_a_name_already_in_use()
    {
        RestoreViewModel viewModel = new("alpha_archive", ["alpha"]) { Name = "alpha" };

        viewModel.NameError.Should().Contain("already exists");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Remove_needs_the_name_typed_back()
    {
        RemoveViewModel viewModel = new("alpha");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();

        viewModel.Typed = "ALPHA";
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
        viewModel.ArchiveFirst = true;
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(new RemoveDto { InstanceName = "alpha", ArchiveFirst = true });
    }

    [Fact]
    public void Restore_trims_the_new_name()
    {
        RestoreViewModel viewModel = new("alpha_archive", []) { Name = "  alpha  " };

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(new RestoreDto
        {
            ArchiveName = "alpha_archive",
            InstanceName = "alpha",
        });
    }

    [Fact]
    public void Delete_archive_needs_the_exact_name()
    {
        DeleteArchiveViewModel viewModel = new("alpha_archive") { Typed = "alpha_archive" };

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be("alpha_archive");
    }

    [Fact]
    public void Docker_register_uses_the_current_state_in_its_lead()
    {
        DockerRegisterViewModel viewModel = new("alpha", "yes");

        viewModel.Lead.Should().Be("UNREGISTER 'ALPHA' FROM DOCKER DESKTOP?");
        viewModel.Restart.Should().BeTrue();

        viewModel.Restart = false;
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(false);
    }

    [Fact]
    public void Recipe_name_validates_then_returns_the_trimmed_name()
    {
        RecipeNameViewModel viewModel = new() { Name = "bad name" };

        viewModel.ConfirmCommand.Execute(null);

        viewModel.NameError.Should().NotBeEmpty();
        viewModel.Result.Should().BeNull();

        viewModel.Name = "  good_1 ";
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be("good_1");
    }

    [Fact]
    public void Pack_duplicate_rejects_taken_names_then_returns_the_answer()
    {
        PackDuplicateViewModel viewModel = new("tools", "Tools", ["tools", "other"]) { Name = "other" };

        viewModel.ConfirmCommand.Execute(null);
        viewModel.NameError.Should().Contain("already exists");

        viewModel.Name = "tools-copy";
        viewModel.Description = " Copy ";
        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be(("tools-copy", "Copy"));
    }

    [Fact]
    public void Root_move_ignores_an_unchanged_or_invalid_target()
    {
        RootMoveViewModel viewModel = new("/fleet") { NewRoot = "/fleet" };

        viewModel.ConfirmCommand.Execute(null);
        viewModel.Result.Should().BeNull();

        viewModel.NewRoot = "relative";
        viewModel.ConfirmCommand.Execute(null);
        viewModel.Result.Should().BeNull();
        viewModel.Warning.Should().NotBeEmpty();
    }

    [Fact]
    public void Root_move_returns_an_absolute_target()
    {
        RootMoveViewModel viewModel = new("/fleet") { NewRoot = "/tmp/wsl-squad-moved" };

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Result.Should().Be("/tmp/wsl-squad-moved");
    }
}
