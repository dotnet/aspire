# frozen_string_literal: true

require "minitest/autorun"
require "open3"
require "fileutils"
require_relative "cask-completion-paths"

class CaskValidationTest < Minitest::Test
  SCRIPT_DIR = File.expand_path(__dir__)
  FAKE_BREW = <<~'RUBY'
    #!/usr/bin/env ruby
    require "json"
    require "fileutils"
    require_relative "../../cask-completion-paths"
    File.open(ENV.fetch("CALLS"), "a") { |f| f.puts JSON.generate(ARGV) }
    behavior = ENV.fetch("BEHAVIOR", "")
    prefix = ENV.fetch("PREFIX")
    case ARGV.first
    when "--prefix"
      puts prefix
    when "info"
      abort "load failed" if behavior == "load_failure"
      warn "DSL is deprecated" if behavior == "load_deprecation"
      puts File.read(ENV.fetch("METADATA"))
    when "ruby"
      args = ARGV.drop(1)
      args.delete("--")
      exec RbConfig.ruby, *args
    when "style"
      abort "style failed" if behavior == "style_failure"
      warn "style deprecation" if behavior == "style_deprecation"
      File.open(ENV.fetch("CASK"), "a") { |f| f.puts "# rewritten" } if behavior == "mutation"
      File.write(File.join(File.dirname(ENV.fetch("CASK")), "added.rb"), "unexpected") if behavior == "added_file"
      File.delete(ENV.fetch("CASK")) if behavior == "deleted_file" && File.exist?(ENV.fetch("CASK"))
    when "fetch"
      abort "checksum mismatch" if behavior == "fetch_failure"
    when "audit"
      abort "audit failed" if behavior == "audit_failure"
    when "install"
      payload = File.join(prefix, "Caskroom/aspire/13.6.1")
      FileUtils.mkdir_p(payload)
      FileUtils.cp(ENV.fetch("FAKE_CLI"), File.join(payload, "aspire"))
      File.symlink(File.join(payload, "aspire"), File.join(prefix, "bin/aspire"))
      File.write(File.join(payload, ".aspire-install.json"),
                 JSON.generate("source" => behavior == "wrong_sidecar" ? "script" : "brew"))
      metadata = JSON.parse(File.read(ENV.fetch("METADATA"))).fetch("casks").first
      CaskCompletionPaths.paths(metadata, prefix).each do |path|
        next if behavior == "missing_completion"
        FileUtils.mkdir_p(File.dirname(path))
        File.write(path, "completion")
      end
    when "uninstall"
      abort "uninstall failed" if behavior == "uninstall_failure"
      unless behavior == "uninstall_residue"
        FileUtils.rm_f(File.join(prefix, "bin/aspire"))
        FileUtils.rm_rf(File.join(prefix, "Caskroom/aspire"))
        metadata = JSON.parse(File.read(ENV.fetch("METADATA"))).fetch("casks").first
        CaskCompletionPaths.paths(metadata, prefix).each { |path| FileUtils.rm_f(path) }
      end
    end
  RUBY
  FAKE_CLI = <<~'RUBY'
    #!/usr/bin/env ruby
    require "json"
    File.open(ENV.fetch("CALLS"), "a") { |f| f.puts JSON.generate(["cli", *ARGV]) }
    if ARGV == ["--version"]
      puts ENV.fetch("BEHAVIOR") == "wrong_version" ? "13.6.0" : "13.6.1+build"
    elsif ARGV.include?("restore") && ENV.fetch("BEHAVIOR") == "restore_failure"
      abort "restore failed"
    end
  RUBY

  def setup
    @work = File.join(SCRIPT_DIR, ".validation-tests-#{Process.pid}-#{object_id}")
    @prefix = File.join(@work, "prefix")
    @tap = File.join(@prefix, "Library/Taps/validation/homebrew-aspire")
    FileUtils.mkdir_p(File.join(@tap, "Casks/a"))
    FileUtils.mkdir_p(File.join(@prefix, "bin"))
    @cask = File.join(@tap, "Casks/a/aspire.rb")
    @template = File.join(@work, "template")
    File.write(@template, "cask \"aspire\" do\n version \"${VERSION}\"\nend\n")
    File.write(@cask, "cask \"aspire\" do\n version \"13.6.1\"\nend\n")
    @metadata = {
      "version" => "13.6.1",
      "url" => "https://github.com/microsoft/aspire/releases/download/v13.6.1/aspire-cli-osx-arm64-13.6.1.tar.gz",
      "sha256" => "a" * 64,
      "variations" => {
        "sequoia" => {
          "url" => "https://github.com/microsoft/aspire/releases/download/v13.6.1/aspire-cli-osx-x64-13.6.1.tar.gz",
          "sha256" => "b" * 64,
        },
      },
      "artifacts" => [],
    }
    @metadata_file = File.join(@work, "metadata.json")
    @calls = File.join(@work, "calls")
    File.write(@calls, "")
    FileUtils.cp(File.join(SCRIPT_DIR, "cask-completion-paths.rb"), @work)
    executable("brew", FAKE_BREW)
    executable("fake-cli", FAKE_CLI)
    executable("dotnet", "#!/bin/sh\necho 10.0.100\n")
    executable("codesign", "#!/bin/sh\nexit 0\n")
    executable("sudo", "#!/bin/sh\nexit 1\n")
  end

  def teardown
    FileUtils.rm_rf(@work)
  end

  def test_prepublication_runs_all_structural_checks_without_live_urls_or_mutation
    before = File.binread(@cask)
    output, status, calls = run_core(mode: "LiveArchives")
    assert status.success?, output
    assert_equal before, File.binread(@cask)
    assert_equal ["audit", "--cask", "validation/aspire/aspire"], calls.last
    assert_equal %w[--prefix info ruby ruby style style readall audit audit], calls.map(&:first)
  end

  def test_published_checks_only_fetches_both_archives_and_online_audits_without_install
    output, status, calls = run_core(extra: ["--checks-only"])
    assert status.success?, output
    assert_includes calls, ["fetch", "--cask", "--retry", "--arch=all", "validation/aspire/aspire"]
    assert_equal ["audit", "--cask", "--online", "validation/aspire/aspire"], calls.last
  end

  def test_structural_failures_and_deprecations_block_installation
    %w[style_failure style_deprecation load_deprecation fetch_failure audit_failure mutation added_file deleted_file].each do |behavior|
      output, status, calls = run_core(behavior: behavior)
      refute status.success?, "#{behavior}: #{output}"
      assert_includes output, "cask checks failed; installation was not attempted"
      assert_equal 0, calls.count { |call| call.first == "install" }
      # Reset deliberate mutation so the next run tests its own failure mode.
      File.write(@cask, "cask \"aspire\" do\n version \"13.6.1\"\nend\n")
      FileUtils.rm_f(File.join(File.dirname(@cask), "added.rb"))
      FileUtils.rm_rf(File.join(@work, "tap-before"))
    end
  end

  def test_load_failure_stops_before_style
    output, status, calls = run_core(behavior: "load_failure")
    refute status.success?
    assert_includes output, "cask loading failed"
    assert_equal %w[--prefix info], calls.map(&:first)
  end

  def test_template_drift_blocks_installation
    File.write(@cask, "cask \"aspire\" do\n version \"13.6.0\"\nend\n")
    output, status, calls = run_core
    refute status.success?
    assert_includes output, "Release-template drift"
    assert_equal 0, calls.count { |call| call.first == "install" }
  end

  def test_prepublication_requires_local_template_instead_of_fetching_unpublished_tag
    output, status, = run_core(mode: "LiveArchives", template: false)
    refute status.success?
    assert_includes output, "LiveArchives requires --release-template"
  end

  def test_full_lifecycle_checks_version_sidecar_scaffold_restore_and_cleanup
    output, status, calls = run_core
    assert status.success?, output
    assert_equal 1, calls.count { |call| call.first == "install" }
    assert_equal 1, calls.count { |call| call.first == "uninstall" }
    assert_equal ["--version", "trace", "trace"], calls.select { |call| call.first == "cli" }.map { |call| call[1] == "--log-level" ? call[2] : call[1] }
    assert calls.any? { |call| call.first == "cli" && call.include?("new") }
    assert calls.any? { |call| call.first == "cli" && call.include?("restore") }
    refute File.exist?(File.join(@prefix, "bin/aspire"))
  end

  def test_lifecycle_failures_propagate_and_attempt_cleanup
    %w[wrong_version wrong_sidecar restore_failure uninstall_residue uninstall_failure].each do |behavior|
      output, status, calls = run_core(behavior: behavior)
      refute status.success?, "#{behavior}: #{output}"
      assert calls.any? { |call| call.first == "uninstall" }, behavior
      FileUtils.rm_f(File.join(@prefix, "bin/aspire"))
      FileUtils.rm_rf(File.join(@prefix, "Caskroom"))
      FileUtils.rm_rf(File.join(@work, "tap-before"))
    end
  end

  def test_declared_completions_are_checked_and_cleaned_up
    @metadata["artifacts"] = [{ "generate_completions_from_executable" => ["aspire", "completions", "script", { "shells" => %w[bash zsh fish] }] }]
    output, status, = run_core
    assert status.success?, output
    assert_equal [], CaskCompletionPaths.paths(@metadata, @prefix).select { |path| File.exist?(path) }
    FileUtils.rm_rf(File.join(@work, "tap-before"))
    output, status, calls = run_core(behavior: "missing_completion")
    refute status.success?
    assert_includes output, "completion file missing or empty after install"
    assert calls.any? { |call| call.first == "uninstall" }
  end

  def test_unknown_completion_metadata_is_not_silently_skipped
    @metadata["artifacts"] = [{ "generate_completions_from_executable" => ["aspire", { "shells" => ["unknown"] }] }]
    output, status, calls = run_core
    refute status.success?, output
    assert_equal 0, calls.count { |call| call.first == "install" }
  end

  def test_snapshot_remains_mandatory_in_ci
    output, status, calls = run_core(extra: ["--snapshot"])
    refute status.success?
    assert_includes output, "Homebrew snapshot/check requires passwordless sudo in CI"
    assert_equal 0, calls.count { |call| call.first == "install" }
  end

  def test_old_release_has_no_completion_requirements_and_current_release_has_all_four
    assert_equal [], CaskCompletionPaths.paths(@metadata, @prefix)
    @metadata["artifacts"] = [
      { "generate_completions_from_executable" => ["aspire", { "shells" => %w[bash zsh fish] }] },
      { "artifact" => ["aspire-completion.ps1"], "target" => "#{@prefix}/share/pwsh/completions/_aspire.ps1" },
    ]
    assert_equal %w[etc/bash_completion.d/aspire share/zsh/site-functions/_aspire share/fish/vendor_completions.d/aspire.fish share/pwsh/completions/_aspire.ps1].map { |path| File.join(@prefix, path) },
                 CaskCompletionPaths.paths(@metadata, @prefix)
  end

  private

  def executable(name, source)
    path = File.join(@prefix, "bin", name)
    File.write(path, source)
    File.chmod(0o755, path)
  end

  def run_core(behavior: "", mode: "LiveRelease", extra: [], template: true)
    File.write(@metadata_file, JSON.generate("casks" => [@metadata]))
    File.write(@calls, "")
    env = {
      "PATH" => "#{@prefix}/bin:#{ENV.fetch("PATH")}", "BEHAVIOR" => behavior,
      "PREFIX" => @prefix, "CALLS" => @calls, "CASK" => @cask, "CI" => "1",
      "METADATA" => @metadata_file, "FAKE_CLI" => File.join(@prefix, "bin/fake-cli"),
    }
    args = ["bash", File.join(SCRIPT_DIR, "validate-cask-core.sh"),
            "--brew", File.join(@prefix, "bin/brew"), "--work-dir", @work,
            "--tap", "validation/aspire", "--cask-file", @cask, "--validation-mode", mode, *extra]
    args += ["--release-template", @template] if template
    output, status = Open3.capture2e(env, *args)
    [output, status, File.readlines(@calls).map { |line| JSON.parse(line) }]
  end
end
