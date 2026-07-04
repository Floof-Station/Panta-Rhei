chat-manager-entity-subtle-wrap-message = [italic][color=#d3d3ff]{ PROPER($entity) ->
    *[false] The [Name]{$entityName}[/Name] {$message}
     [true] [Name]{$entityName}[/Name] {$message}
}[/color][/italic]

chat-manager-entity-subtle-looc-wrap-message = [italic][color=#ff7782]SOOC: [Name]{$entityName}[/Name]: {$message}[/color][/italic]

# Shows a LanguageIconTag, for use in other Fluent strings.
# Note: this has to contain both an opening tag and a closing tag, and the tag cannot be self-closing, because otherwise Robust will skip calling either BeforeText or AfterText
chat-manager-language-hint = { $language ->
    [null] {""}
    *[other] {"["}langicon="{$language}"][/langicon]
}
# Simple ($language) wrapper.
chat-manager-language-hint-ui = {" "}({$language})

chat-manager-language-requires-hands = You need at least one free hand to speak this language!
chat-manager-language-requires-speech = You are unable to speak right now!

# todo move this wherever it belongs
# Preferably create a separate file
chat-speech-verb-marish = Mars

chat-speech-verb-name-oldvox = Old-Kin
chat-speech-verb-oldvox-1 = croaks
chat-speech-verb-oldvox-2 = rasps
chat-speech-verb-oldvox-3 = wheezes
chat-speech-verb-oldvox-4 = clicks
chat-speech-verb-oldvox-5 = chirps
chat-speech-verb-oldvox-6 = sings
