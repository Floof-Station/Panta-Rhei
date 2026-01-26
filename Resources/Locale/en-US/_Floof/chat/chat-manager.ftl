chat-manager-entity-subtle-wrap-message = [italic][color=#d3d3ff]{ PROPER($entity) ->
    *[false] The [Name]{$entityName}[/Name] {$message}
     [true] [Name]{$entityName}[/Name] {$message}
}[/color][/italic]

chat-manager-entity-subtle-looc-wrap-message = [italic][color=#ff7782]SOOC: [Name]{$entityName}[/Name]: {$message}[/color][/italic]

# A hint about the language of this chat message, meant for embedded use in other Fluent strings related to the chat manager.
# This evaluates to an empty string if the variable $language is the literal string "null" (handled by ChatSystem.LanguageNameForFluent)
# If the variable $language contains anything else, it evaluates to the string "in $language"
#
# Example usage: `local-chat-wrap = {$name} says, "{$message}"{chat-manager-language-hint}.`
# (notice the lack of space before the ref to this variable)
chat-manager-language-hint = { $language ->
    [null] {""}
    *[other] {" "}in [BubbleLanguage][color={$textColor}]{$language}[/color][/BubbleLanguage]
}
# Simple ($language) wrapper.
chat-manager-language-hint-ui = {" "}({$language})

chat-manager-language-requires-hands = You need free hands to speak this language!
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
