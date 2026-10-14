# frozen_string_literal: true

# Compare the supplied cask with its rendered release template without executing
# either Ruby source. Formatting may differ; syntax and literal values may not.
require "json"
require "ripper"

module CaskConsistency
  # Ripper parses Ruby into nested arrays; reject invalid input before comparing.
  def self.syntax_tree(source)
    tree = Ripper.sexp(source)
    raise ArgumentError, "Invalid Ruby syntax in cask source" unless tree

    normalize_tree(tree)
  end

  # Terminal nodes carry source coordinates, which change with harmless layout.
  def self.normalize_tree(node)
    return node unless node.is_a?(Array)
    return node.first(2) if node.first.is_a?(Symbol) && node.first.to_s.start_with?("@")

    node.map { |child| normalize_tree(child) }
  end

  # Keep literal spelling and DSL arguments that syntax-tree comparison alone
  # can normalize, while discarding comments and layout tokens.
  def self.tokens(source)
    Ripper.lex(source).each_with_object([]) do |(_, type, text, _), tokens|
      tokens << [type, text] unless [
        :on_sp, :on_nl, :on_ignored_nl, :on_comment,
        :on_embdoc_beg, :on_embdoc, :on_embdoc_end
      ].include?(type)
    end
  end

  # Both checks are needed: tokens preserve spelling, trees preserve statements.
  def self.matches?(expected, actual)
    expected_tree = syntax_tree(expected)
    actual_tree = syntax_tree(actual)
    expected_tree == actual_tree && tokens(expected) == tokens(actual)
  end

  # Fill release placeholders using the version and both archive hashes reported
  # by Homebrew, rather than maintaining another copy of release metadata.
  def self.render(template, metadata)
    version = metadata.fetch("version")
    raise ArgumentError, "Invalid release version: #{version.inspect}" unless version.match?(/\A\d+\.\d+\.\d+(?:[-.][a-zA-Z0-9.-]+)?\z/)

    replacements = { "VERSION" => version }
    variants = [metadata] + metadata.fetch("variations", {}).values.map { |value| metadata.merge(value) }
    { "ARM64" => "arm64", "X64" => "x64" }.each do |key, arch|
      url = "https://github.com/microsoft/aspire/releases/download/v#{version}/aspire-cli-osx-#{arch}-#{version}.tar.gz"
      hashes = variants.each_with_object([]) do |variant, results|
        results << variant["sha256"] if variant["url"] == url && variant["sha256"]
      end.uniq
      if hashes.length != 1 || !hashes.first.match?(/\A[0-9a-f]{64}\z/)
        raise ArgumentError, "Missing or conflicting macOS #{arch} SHA256 in Homebrew metadata"
      end

      replacements["SHA256_OSX_#{key}"] = hashes.first
    end
    template.gsub(/\$\{([^}]+)\}/) do
      replacements.fetch(Regexp.last_match(1)) do |key|
        raise ArgumentError, "Unknown release-template placeholder: #{key}"
      end
    end
  end
end

if $PROGRAM_NAME == __FILE__
  abort "Usage: ruby #{$PROGRAM_NAME} CASK METADATA_JSON TEMPLATE" if ARGV.length != 3
  cask_path, metadata_path, template_path = ARGV
  metadata = JSON.parse(File.read(metadata_path)).fetch("casks").fetch(0)
  expected = CaskConsistency.render(File.read(template_path), metadata)
  actual = File.read(cask_path)
  unless CaskConsistency.matches?(expected, actual)
    warn "Release-template drift: #{cask_path} does not match microsoft/aspire v#{metadata.fetch("version")}."
    warn "Comparison ignores comments and layout, but preserves Ruby structure, strings and DSL arguments."
    warn "Expected: #{CaskConsistency.tokens(expected).map(&:last).join(" ")}"
    warn "Actual:   #{CaskConsistency.tokens(actual).map(&:last).join(" ")}"
    exit 1
  end
  puts "Cask matches release template (ignoring comments and layout)."
end
