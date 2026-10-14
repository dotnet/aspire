# frozen_string_literal: true

require "json"

module CaskCompletionPaths
  def self.paths(metadata, prefix)
    locations = {
      "bash" => "etc/bash_completion.d/aspire",
      "zsh" => "share/zsh/site-functions/_aspire",
      "fish" => "share/fish/vendor_completions.d/aspire.fish",
      "pwsh" => "share/pwsh/completions/_aspire.ps1",
    }
    metadata.fetch("artifacts").flat_map do |artifact|
      if artifact.key?("generate_completions_from_executable")
        # info --json=v2 emits ["aspire", "completions", "script",
        # {"shells": ["bash", "zsh", "fish"], ...}] for the native artifact.
        options = artifact.fetch("generate_completions_from_executable").last
        options.fetch("shells").map { |shell| File.join(prefix, locations.fetch(shell)) }
      elsif artifact.key?("artifact") && artifact["target"] == File.join(prefix, locations.fetch("pwsh"))
        [artifact.fetch("target")]
      else
        []
      end
    end.uniq
  end
end

if $PROGRAM_NAME == __FILE__
  abort "Usage: ruby #{$PROGRAM_NAME} METADATA_JSON PREFIX" unless ARGV.length == 2
  metadata = JSON.parse(File.read(ARGV.fetch(0))).fetch("casks").fetch(0)
  puts CaskCompletionPaths.paths(metadata, ARGV.fetch(1))
end
