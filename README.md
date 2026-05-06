# AnimationPatches

A Unity Editor tool that improves the Animation Window clip selection experience.

## What it does

Replaces the default animation clip dropdown in the Animation Window with a searchable list that shows:
- Clip names with search/filter support
- Full asset paths in tooltips
- Better navigation for projects with many animation clips

## Installation

Add this package to your Unity project's `Packages` folder.

## Requirements

- Unity 2022.3 or compatible version
- **HarmonyLib** - Required dependency:
  - For VRChat projects: Already included in VRChat SDK
  - For regular Unity projects: Add [HarmonyLib](https://github.com/pardeike/Harmony) manually

## Usage

1. Open Animation Window (Window > Animation > Animation)
2. Select a GameObject with an Animator component
3. Click the clip dropdown - you'll see the improved searchable interface

---

# AnimationPatches

Инструмент для Unity Editor, улучшающий выбор анимационных клипов в Animation Window.

## Что делает

Заменяет стандартный dropdown выбора анимационных клипов в Animation Window на список с поиском, который показывает:
- Названия клипов с поддержкой поиска/фильтрации
- Полные пути к ассетам в подсказках
- Улучшенную навигацию для проектов с большим количеством анимационных клипов

## Установка

Добавьте этот пакет в папку `Packages` вашего Unity проекта.

## Требования

- Unity 2022.3 или совместимая версия
- **HarmonyLib** - Обязательная зависимость:
  - Для VRChat проектов: Уже включена в VRChat SDK
  - Для обычных Unity проектов: Добавьте [HarmonyLib](https://github.com/pardeike/Harmony) вручную

## Использование

1. Откройте Animation Window (Window > Animation > Animation)
2. Выберите GameObject с компонентом Animator
3. Кликните на dropdown выбора клипа - вы увидите улучшенный интерфейс с поиском
