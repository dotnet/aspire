# frozen_string_literal: true

# Exercise the shared comparator's Ruby parsing and release-metadata substitution.
# These standard-library tests need neither Homebrew nor downloaded archives.
require "minitest/autorun"
require_relative "check-cask-consistency"

class CaskConsistencyTest < Minitest::Test
  def test_comments_and_layout_do_not_count_as_drift
    original = "cask \"aspire\" do\n  zap trash: []\nend\n"
    formatted = "# frozen_string_literal: true\ncask \"aspire\" do\n\n zap  trash: [] # shared state\nend\n"
    assert CaskConsistency.matches?(original, formatted)
  end

  def test_install_and_uninstall_changes_count_as_drift
    original = 'write_file ".aspire-install.json", "{}", base: :staged_path'
    refute_equal CaskConsistency.tokens(original),
                 CaskConsistency.tokens('write_file ".aspire-install.json", "{}"')
    refute_equal CaskConsistency.tokens("zap trash: []"),
                 CaskConsistency.tokens('zap trash: "~/.aspire"')
  end

  def test_block_comments_do_not_count_as_drift
    original = "cask \"aspire\" do\n  zap trash: []\nend\n"
    documented = "=begin\nShared user state is retained.\n=end\n#{original}"
    assert CaskConsistency.matches?(original, documented)
  end

  def test_block_comment_markers_in_strings_remain_significant
    original = "desc \"=begin\nAspire CLI\n=end\"\n"
    changed = "desc \"=begin\nDifferent CLI\n=end\"\n"
    refute_equal CaskConsistency.tokens(original), CaskConsistency.tokens(changed)
  end

  def test_statement_terminating_newline_counts_as_drift
    refute CaskConsistency.matches?("binary \"aspire\"\n", "binary\n\"aspire\"\n")
  end

  def test_continuations_and_trailing_comments_do_not_count_as_drift
    original = "binary \"aspire\"\n"
    continued = "binary \\\n  \"aspire\" # installed command\n\n"
    assert CaskConsistency.matches?(original, continued)
    assert CaskConsistency.matches?(original, 'binary "aspire"')
  end

  def test_literal_spelling_remains_significant
    refute CaskConsistency.matches?('binary "aspire"', "binary 'aspire'")
    refute CaskConsistency.matches?('desc "Aspire CLI"', 'desc "Aspire  CLI"')
  end

  def test_invalid_ruby_is_an_error
    invalid = 'cask "aspire" do'
    valid = 'binary "aspire"'
    [[invalid, valid], [valid, invalid], [invalid, invalid]].each do |expected, actual|
      error = assert_raises(ArgumentError) { CaskConsistency.matches?(expected, actual) }
      assert_match "Invalid Ruby syntax", error.message
    end
  end

  def test_deprecated_url_metadata_counts_as_drift
    refute_equal CaskConsistency.tokens('url "https://example.com/archive"'),
                 CaskConsistency.tokens('url "https://example.com/archive", verified: "example.com/"')
  end

  def test_string_contents_are_not_normalized
    refute_equal CaskConsistency.tokens('desc "Aspire CLI"'),
                 CaskConsistency.tokens('desc "Aspire  CLI"')
  end

  def test_renders_both_architectures_from_homebrew_metadata
    arm = "a" * 64
    intel = "b" * 64
    metadata = {
      "version"    => "13.6.1",
      "url"        => archive_url("arm64"),
      "sha256"     => arm,
      "variations" => {
        "sequoia"     => { "url" => archive_url("x64"), "sha256" => intel },
        "arm64_linux" => { "sha256" => nil },
      },
    }
    template = "${VERSION} ${SHA256_OSX_ARM64} ${SHA256_OSX_X64}"
    assert_equal "13.6.1 #{arm} #{intel}", CaskConsistency.render(template, metadata)
    metadata["url"] = archive_url("x64")
    metadata["sha256"] = intel
    metadata["variations"] = {
      "arm64_sequoia" => { "url" => archive_url("arm64"), "sha256" => arm },
    }
    assert_equal "13.6.1 #{arm} #{intel}", CaskConsistency.render(template, metadata)
  end

  def test_missing_architecture_is_an_error
    metadata = { "version" => "13.6.1", "url" => archive_url("arm64"), "sha256" => "a" * 64 }
    assert_raises(ArgumentError) { CaskConsistency.render("${VERSION}", metadata) }
  end

  def test_unknown_template_placeholder_is_an_error
    metadata = {
      "version" => "13.6.1", "url" => archive_url("arm64"), "sha256" => "a" * 64,
      "variations" => { "sequoia" => { "url" => archive_url("x64"), "sha256" => "b" * 64 } }
    }
    assert_raises(ArgumentError) { CaskConsistency.render("${UNKNOWN}", metadata) }
  end

  private

  def archive_url(arch)
    "https://github.com/microsoft/aspire/releases/download/v13.6.1/aspire-cli-osx-#{arch}-13.6.1.tar.gz"
  end
end
