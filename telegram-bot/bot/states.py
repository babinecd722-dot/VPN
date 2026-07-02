from aiogram.fsm.state import State, StatesGroup


class OsintStates(StatesGroup):
    waiting_query = State()


class ChatStates(StatesGroup):
    waiting_message = State()
