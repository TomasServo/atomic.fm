from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from ifthen.flow import FlowEngine, FlowError, FlowScript, UserContext, matches_condition

FLOWS = ROOT / "flows" / "atomic.fm.json"


class ConditionTests(unittest.TestCase):
    def test_empty_condition_is_visible(self):
        self.assertTrue(matches_condition(None, UserContext()))

    def test_has_role_name(self):
        dj = UserContext(role_names=frozenset({"DJ"}))
        self.assertTrue(matches_condition({"has_role": "DJ"}, dj))
        self.assertFalse(matches_condition({"has_role": "DJ"}, UserContext()))

    def test_has_role_id(self):
        user = UserContext(role_ids=frozenset({"123"}))
        self.assertTrue(matches_condition({"has_any_role": ["123", "DJ"]}, user))

    def test_missing_role(self):
        self.assertFalse(
            matches_condition({"missing_role": "Muted"}, UserContext(role_names=frozenset({"Muted"})))
        )

    def test_user_id(self):
        self.assertTrue(matches_condition({"user_id": "42"}, UserContext(user_id="42")))
        self.assertFalse(matches_condition({"user_id": "42"}, UserContext(user_id="7")))


class ScriptTests(unittest.TestCase):
    def test_atomic_flow_loads(self):
        script = FlowScript.from_path(FLOWS)
        self.assertEqual(script.name, "atomic-fm")
        self.assertIn("root", script.nodes)

    def test_missing_then_target_is_invalid(self):
        with self.assertRaises(FlowError):
            FlowScript.from_dict(
                {
                    "name": "bad",
                    "start": "a",
                    "nodes": {
                        "a": {
                            "content": "hi",
                            "buttons": [{"id": "x", "label": "X", "then": "nowhere"}],
                        }
                    },
                }
            )

    def test_duplicate_button_ids_are_invalid(self):
        with self.assertRaises(FlowError):
            FlowScript.from_dict(
                {
                    "name": "bad",
                    "start": "a",
                    "nodes": {
                        "a": {
                            "content": "hi",
                            "buttons": [
                                {"id": "x", "label": "One", "then": "a"},
                                {"id": "x", "label": "Two", "then": "a"},
                            ],
                        }
                    },
                }
            )


class EngineTests(unittest.TestCase):
    def setUp(self):
        self.engine = FlowEngine(FlowScript.from_path(FLOWS))

    def test_guest_does_not_see_dj_button(self):
        node = self.engine.start(UserContext())
        ids = [button.id for button in node.buttons]
        self.assertIn("listen", ids)
        self.assertNotIn("dj", ids)

    def test_dj_sees_dj_button(self):
        node = self.engine.start(UserContext(role_names=frozenset({"DJ"})))
        self.assertIn("dj", [button.id for button in node.buttons])

    def test_listen_then_sound_block(self):
        user = UserContext()
        after_listen = self.engine.click("root", "listen", user)
        self.assertEqual(after_listen.id, "listen")
        after_sound = self.engine.click("listen", "sound", user)
        self.assertEqual(after_sound.id, "sound_block")
        self.assertIn("atomic.fm=true", after_sound.content)

    def test_hidden_button_cannot_be_clicked(self):
        with self.assertRaises(FlowError):
            self.engine.click("root", "dj", UserContext())

    def test_broken_path_to_volume(self):
        user = UserContext()
        silent = self.engine.click("root", "broken", user)
        quiet = self.engine.click(silent.id, "quiet", user)
        self.assertEqual(quiet.id, "volume")


class JsonRoundTripTests(unittest.TestCase):
    def test_flow_is_valid_json_object(self):
        data = json.loads(FLOWS.read_text(encoding="utf-8"))
        self.assertIsInstance(data["nodes"], dict)


if __name__ == "__main__":
    unittest.main()
