import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

class AppWordmark extends StatelessWidget {
  const AppWordmark({super.key, this.compact = false});
  final bool compact;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Image.asset(
        'assets/branding/upts-logo.png',
        width: compact ? 32 : 36,
        height: compact ? 32 : 36,
        fit: BoxFit.contain,
        semanticLabel: 'UPTSLK logo',
      ),
      const SizedBox(width: 10),
      Text(
        'UPTSLK',
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
          color: AppTheme.ink,
          fontWeight: FontWeight.w800,
          letterSpacing: -0.3,
        ),
      ),
    ],
  );
}

class AppPageTitle extends StatelessWidget {
  const AppPageTitle({
    super.key,
    required this.title,
    this.subtitle,
    this.trailing,
  });

  final String title;
  final String? subtitle;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.headlineMedium),
            if (subtitle != null) ...[
              const SizedBox(height: 5),
              Text(
                subtitle!,
                style: Theme.of(context).textTheme.bodyMedium
                    ?.copyWith(color: AppTheme.muted),
              ),
            ],
          ],
        ),
      ),
      if (trailing != null) ...[const SizedBox(width: 16), trailing!],
    ],
  );
}

class AppSurface extends StatelessWidget {
  const AppSurface({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.radius = AppTheme.radiusLarge,
    this.color = AppTheme.surface,
    this.width,
    this.margin,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final double radius;
  final Color color;
  final double? width;
  final EdgeInsetsGeometry? margin;

  @override
  Widget build(BuildContext context) => Container(
    width: width,
    margin: margin,
    padding: padding,
    decoration: BoxDecoration(
      color: color,
      border: Border.all(color: AppTheme.border),
      borderRadius: BorderRadius.circular(radius),
    ),
    child: child,
  );
}

class AppSectionTitle extends StatelessWidget {
  const AppSectionTitle({super.key, required this.title, this.trailing});
  final String title;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(title, style: Theme.of(context).textTheme.titleMedium),
      ),
      ?trailing,
    ],
  );
}

class AppBottomNavigation extends StatelessWidget {
  const AppBottomNavigation({
    super.key,
    required this.selectedIndex,
    required this.onSelected,
    required this.destinations,
  });

  final int selectedIndex;
  final ValueChanged<int> onSelected;
  final List<NavigationDestination> destinations;

  @override
  Widget build(BuildContext context) => DecoratedBox(
    decoration: const BoxDecoration(
      color: Color(0xFFEDECF8),
      border: Border(top: BorderSide(color: Color(0xFFD9D6EB))),
    ),
    child: SafeArea(
      top: false,
      child: SizedBox(
        height: 66,
        child: Row(
          children: [
            for (var index = 0; index < destinations.length; index++)
              Expanded(
                child: _BottomNavigationItem(
                  destination: destinations[index],
                  selected: selectedIndex == index,
                  onTap: () => onSelected(index),
                ),
              ),
          ],
        ),
      ),
    ),
  );
}

class _BottomNavigationItem extends StatelessWidget {
  const _BottomNavigationItem({
    required this.destination,
    required this.selected,
    required this.onTap,
  });

  final NavigationDestination destination;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Semantics(
    selected: selected,
    button: true,
    label: destination.label,
    child: Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 220),
              curve: Curves.easeOutCubic,
              width: 52,
              height: 30,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? AppTheme.brandPrimary : Colors.transparent,
                borderRadius: BorderRadius.circular(12),
              ),
              child: IconTheme(
                data: IconThemeData(
                  color: selected ? Colors.white : AppTheme.muted,
                  size: 21,
                ),
                child: selected
                    ? destination.selectedIcon ?? destination.icon
                    : destination.icon,
              ),
            ),
            const SizedBox(height: 3),
            Text(
              destination.label,
              style: TextStyle(
                color: selected ? AppTheme.brandPrimary : AppTheme.muted,
                fontSize: 11,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
