namespace TeleBot.TestConsole;

public static class Constants
{
    public const string JsonUpdateMessage = """
                                            {
                                                "update_id": 96421762,
                                                "message": {
                                                    "message_id": 301,
                                                    "from": {
                                                        "id": 81123446,
                                                        "is_bot": false,
                                                        "first_name": "Ben",
                                                        "username": "durov"
                                                    },
                                                    "chat": {
                                                        "id": -1032015434551,
                                                        "title": "TITLE",
                                                        "type": "supergroup"
                                                    },
                                                    "date": 1712128178,
                                                    "text": "https://vm.tiktok.com/ZGeXBrXjc/",
                                                    "entities": [
                                                        {
                                                            "offset": 0,
                                                            "length": 32,
                                                            "type": "url"
                                                        }
                                                    ],
                                                    "link_preview_options": {
                                                        "is_disabled": true
                                                    }
                                                }
                                            }
                                            """;
}
